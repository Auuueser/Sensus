using System;

namespace Sensus.Captions;

// Configuration is not an allocation size. Existing capture/storage overload guards
// remain independent of the user's requested display count.
internal static class DisplayCapacity
{
    internal const int SignalSafety = 64;
    internal static int Resolve(int requested, int available) => Math.Min(requested <= 0 ? available : requested, Math.Max(0, available));
    internal static int Rows(float height, float rowHeight) => float.IsNaN(height) || float.IsInfinity(height) || float.IsNaN(rowHeight) || float.IsInfinity(rowHeight) || rowHeight <= 0 || height <= 0
        ? 0 : (int)Math.Min(SignalSafety, Math.Floor(height / rowHeight));
    internal struct Slot { internal float X, Y, Angle; }
    // A non-overlapping perimeter grid leaves the center clear. Slot positions do
    // not depend on the number of active sounds, so changing counts cannot squeeze
    // existing labels together. Reserved area is in the same centered coordinates.
    internal static int Perimeter(Slot[] slots, float width, float height, float cellWidth, float cellHeight,
        float reserveLeft, float reserveBottom, float reserveRight, float reserveTop)
    {
        int columns=Rows(width,cellWidth), rows=Rows(height,cellHeight), count=0;
        if(slots.Length==0 || columns<2 || rows<2) return 0;
        // A centered top/bottom cell prevents even grids from leaving a gap in
        // the directly forward/behind sectors at large label sizes.
        if(columns>2 && columns%2==0) columns--;
        for(int y=0;y<rows;y++) for(int x=0;x<columns;x++)
        {
            if(x!=0 && y!=0 && x!=columns-1 && y!=rows-1) continue;
            float px=(x-(columns-1)*0.5f)*cellWidth, py=(y-(rows-1)*0.5f)*cellHeight;
            if(px+cellWidth*0.5f>reserveLeft && px-cellWidth*0.5f<reserveRight &&
                py+cellHeight*0.5f>reserveBottom && py-cellHeight*0.5f<reserveTop) continue;
            slots[count++]=new Slot { X=px,Y=py,Angle=(float)(Math.Atan2(px,py)*180/Math.PI) };
            if(count==slots.Length) return count;
        }
        return count;
    }
}
