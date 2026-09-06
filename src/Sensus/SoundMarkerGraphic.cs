using Sensus.Captions;
using UnityEngine;
using UnityEngine.UI;

namespace Sensus;

// Own UGUI mesh, not font glyphs or a second Canvas. Native text sits alongside.
internal sealed class SoundMarkerGraphic : MaskableGraphic
{
    internal static Color Tint(Cue cue, float alpha)
    {
        var rgb = CueText.Priority(cue) switch
        {
            3 => new Color(1f,0.25f,0.22f),
            2 => new Color(1f,0.8f,0.25f),
            _ => new Color(0.96f,0.96f,0.92f)
        };
        rgb.a=alpha; return rgb;
    }
    private float angle;
    private Cue cue;
    private int strength;
    private bool compact;
    private bool directional = true;
    internal void Set(float bearing, Cue value, int level, bool arcOnly, bool hasDirection = true)
    {
        if (angle == bearing && cue == value && strength == level && compact == arcOnly && directional == hasDirection) return;
        angle = bearing; cue = value; strength = level; compact = arcOnly; directional = hasDirection; SetVerticesDirty();
    }
    public override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 64f;
        // Distinct recordings combine a species silhouette with the existing event symbol.
        // Direction and intensity remain full-size and independent of this composition.
        var cue=CreatureSoundCatalog.Basis(this.cue);
        var shape=CreatureSoundCatalog.Shape(this.cue);
        // Dedicated tracks use the whole glyph area instead of a small head + shoe.
        bool birdTracks=this.cue==Cue.DetailBaboonBirdAIFootsteps;
        if(birdTracks) shape=CreatureShape.None;
        float glyphScale=1, offset=0;
        Vector2 P(float x, float y) => new Vector2(x*glyphScale+offset, y*glyphScale) * scale;
        // Coverage fringe in screen pixels. No texture sampling or per-frame atlas.
        float fringe=1f/Mathf.Max(0.01f,(canvas!=null ? canvas.scaleFactor : 1)*rectTransform.localScale.x);
        Color clear=color; clear.a=0;
        void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color ca,Color cb,Color cc,Color cd)
        {
            int i=vh.currentVertCount;
            vh.AddVert(a,ca,Vector2.zero); vh.AddVert(b,cb,Vector2.zero);
            vh.AddVert(c,cc,Vector2.zero); vh.AddVert(d,cd,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
        void Line(Vector2 a, Vector2 b, float width = 3f)
        {
            Vector2 tangent=(b-a).normalized;
            Vector2 normal=new Vector2(-tangent.y,tangent.x);
            Vector2 n=normal*width*scale*glyphScale*0.5f, outer=n+normal*fringe;
            Quad(a-n,a+n,b+n,b-n,color,color,color,color);
            Quad(a+n,a+outer,b+outer,b+n,color,clear,clear,color);
            Quad(a-outer,a-n,b-n,b-outer,clear,color,color,clear);
            Quad(a-outer-tangent*fringe,a+outer-tangent*fringe,a+n,a-n,clear,clear,color,color);
            Quad(b-n,b+n,b+outer+tangent*fringe,b-outer+tangent*fringe,color,color,clear,clear);
        }
        void Oval(float x,float y,float rx,float ry,int segments=40,bool filled=false)
        {
            segments=Mathf.Max(40,segments);
            for(int i=0;i<segments;i++)
            {
                float a=i*2*Mathf.PI/segments, b=(i+1)*2*Mathf.PI/segments;
                Vector2 pa=P(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),pb=P(x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry);
                Vector2 na=new Vector2(Mathf.Cos(a)/rx,Mathf.Sin(a)/ry).normalized;
                Vector2 nb=new Vector2(Mathf.Cos(b)/rx,Mathf.Sin(b)/ry).normalized;
                float half=1.25f*scale*glyphScale;
                Vector2 ia=filled ? P(x,y) : pa-na*half, ib=filled ? P(x,y) : pb-nb*half;
                Quad(ia,pa+na*half,pb+nb*half,ib,color,color,color,color);
                Quad(pa+na*half,pa+na*(half+fringe),pb+nb*(half+fringe),pb+nb*half,color,clear,clear,color);
                if(!filled) Quad(ia-na*fringe,ia,ib,ib-nb*fringe,clear,color,color,clear);
            }
        }
        void Curve(Vector2 a,Vector2 control,Vector2 b,float width=2.5f)
        {
            Vector2 prior=a;
            for(int i=1;i<=24;i++)
            {
                float t=i/24f; Vector2 next=(1-t)*(1-t)*a+2*(1-t)*t*control+t*t*b;
                Line(prior,next,width); prior=next;
            }
        }
        void Shoe(float x,float y,float rotation,float size=1)
        {
            float cos=Mathf.Cos(rotation*Mathf.Deg2Rad),sin=Mathf.Sin(rotation*Mathf.Deg2Rad);
            Vector2 Point(float t,bool heel)
            {
                float px=Mathf.Sin(t)*(heel ? 3f : 4.2f+0.7f*Mathf.Cos(t));
                float py=heel ? Mathf.Min(-6.5f,-9f+3.5f*Mathf.Cos(t)) : Mathf.Max(-4f,7f*Mathf.Cos(t));
                return P(x+size*(px*cos-py*sin),y+size*(px*sin+py*cos));
            }
            for(int part=0;part<2;part++)
            {
                bool heel=part==1;
                float centerY=heel ? -9 : 1;
                Vector2 center=P(x-size*centerY*sin,y+size*centerY*cos);
                for(int i=0;i<48;i++)
                {
                    float a=i*2*Mathf.PI/48,b=(i+1)*2*Mathf.PI/48;
                    Vector2 pa=Point(a,heel),pb=Point(b,heel);
                    Vector2 ta=Point(a+0.01f,heel)-Point(a-0.01f,heel);
                    Vector2 tb=Point(b+0.01f,heel)-Point(b-0.01f,heel);
                    Vector2 na=new Vector2(-ta.y,ta.x).normalized*fringe;
                    Vector2 nb=new Vector2(-tb.y,tb.x).normalized*fringe;
                    Quad(center,pa,pb,center,color,color,color,color);
                    Quad(pa,pa+na,pb+nb,pb,color,clear,clear,color);
                }
            }
        }
        Vector2 Arc(float degrees) => P(Mathf.Cos(degrees*Mathf.Deg2Rad)*28, Mathf.Sin(degrees*Mathf.Deg2Rad)*28);
        if(directional) for (int j = 0; j < 24; j++) Line(Arc(65-angle+j*50f/24), Arc(65-angle+(j+1)*50f/24), CueText.Priority(cue)==3 ? 4 : 3);
        if (compact) return;
        var family=CueVisual.Family(cue);
        void Box(float x,float y,float w,float h)
        { Line(P(x,y),P(x+w,y)); Line(P(x+w,y),P(x+w,y+h)); Line(P(x+w,y+h),P(x,y+h)); Line(P(x,y+h),P(x,y)); }
        void Wave(float x,float y)
        { Curve(P(x,y),P(x+7,y+5),P(x+14,y)); Curve(P(x+14,y),P(x+21,y-5),P(x+28,y)); }
        if(shape!=CreatureShape.None)
        {
            glyphScale=0.44f; offset=-18;
            switch(shape)
            {
                case CreatureShape.MouthDogAI:
                    Curve(P(-16,8),P(-8,20),P(13,5)); Line(P(13,5),P(-11,0));
                    Line(P(-11,0),P(12,-9)); Line(P(12,-9),P(-9,-15));
                    for(int x=-7;x<=7;x+=7) Line(P(x,2),P(x,-3)); break;
                case CreatureShape.SandWormAI:
                    Oval(0,10,12,7); Curve(P(-12,10),P(-17,-11),P(8,-17));
                    Curve(P(12,10),P(9,-4),P(8,-17)); Line(P(-6,12),P(0,5)); Line(P(0,5),P(6,12)); break;
                case CreatureShape.JesterAI:
                    Box(-13,-17,26,21); Line(P(-13,4),P(-7,17)); Line(P(-7,17),P(0,7));
                    Line(P(0,7),P(9,17)); Line(P(9,17),P(13,4)); Line(P(13,-5),P(19,-5)); Line(P(19,-5),P(19,1)); break;
                case CreatureShape.NutcrackerEnemyAI:
                    Box(-10,5,20,13); Line(P(-14,4),P(14,4)); Box(-9,-14,18,17);
                    Oval(0,-3,3,3); Line(P(-7,-10),P(7,-10)); break;
                case CreatureShape.CaveDwellerAI:
                    Oval(0,3,14,16); Oval(-6,5,3,4); Oval(6,5,3,4);
                    Curve(P(-6,-7),P(0,-12),P(6,-7)); Line(P(-12,-9),P(-18,-16)); Line(P(12,-9),P(18,-16)); break;
                case CreatureShape.DressGirlAI:
                    Oval(0,10,6,8); Line(P(-6,12),P(-12,-1)); Line(P(6,12),P(12,-1));
                    Line(P(0,2),P(-12,-16)); Line(P(-12,-16),P(12,-16)); Line(P(12,-16),P(0,2)); break;
                case CreatureShape.ClaySurgeonAI:
                    Oval(0,11,5,7); Line(P(0,4),P(0,-12)); Line(P(-12,0),P(12,0));
                    Line(P(0,-12),P(-9,-20)); Line(P(0,-12),P(9,-20)); Line(P(9,0),P(17,8)); Line(P(9,8),P(17,0)); break;
                case CreatureShape.SpringManAI:
                    Oval(0,12,7,7); Line(P(-6,5),P(6,-1)); Line(P(6,-1),P(-6,-6));
                    Line(P(-6,-6),P(6,-11)); Line(P(-12,-12),P(12,-12)); Line(P(-8,-12),P(-8,-19)); Line(P(8,-12),P(8,-19)); break;
                case CreatureShape.FlowermanAI:
                    Oval(0,1,7,12); Line(P(-4,4),P(-1,4)); Line(P(1,4),P(4,4));
                    for(int y=-12;y<=12;y+=8) { Line(P(-6,y),P(-15,y+5)); Line(P(6,y),P(15,y+5)); } break;
                case CreatureShape.CrawlerAI:
                    Oval(0,8,8,7); Line(P(-8,2),P(-16,-11)); Line(P(-16,-11),P(-7,-17));
                    Line(P(8,2),P(16,-11)); Line(P(16,-11),P(7,-17)); Line(P(0,1),P(0,-12)); break;
                case CreatureShape.CentipedeAI:
                    Oval(0,10,7,7); Oval(0,-3,6,6); Oval(0,-14,4,4);
                    for(int y=-12;y<=10;y+=8) { Line(P(-6,y),P(-13,y-4)); Line(P(6,y),P(13,y-4)); } break;
                case CreatureShape.SandSpiderAI:
                    Oval(0,-3,7,10); Oval(0,10,5,5);
                    for(int y=-12;y<=12;y+=8) { Line(P(-5,y/2),P(-15,y)); Line(P(-15,y),P(-19,y-5)); Line(P(5,y/2),P(15,y)); Line(P(15,y),P(19,y-5)); } break;
                case CreatureShape.HoarderBugAI:
                    Oval(0,-3,9,12); Oval(0,11,5,5); Line(P(-3,15),P(-9,21)); Line(P(3,15),P(9,21));
                    Line(P(-8,2),P(-15,-4)); Line(P(8,2),P(15,-4)); Line(P(-5,-14),P(-9,-19)); Line(P(5,-14),P(9,-19)); break;
                case CreatureShape.BlobAI:
                    Curve(P(-17,-12),P(-12,21),P(0,10)); Curve(P(0,10),P(15,22),P(18,-12));
                    Curve(P(18,-12),P(0,-19),P(-17,-12)); Oval(-5,0,2,3); Oval(5,0,2,3); break;
                case CreatureShape.PufferAI:
                    Oval(-3,0,11,12); Curve(P(8,0),P(21,15),P(18,-10));
                    Line(P(-8,10),P(-11,18)); Line(P(-5,-10),P(-13,-16)); Line(P(4,-9),P(9,-16)); break;
                case CreatureShape.ButlerEnemyAI:
                    Oval(0,11,6,7); Box(-11,-18,22,21); Line(P(0,-1),P(-6,2)); Line(P(-6,2),P(-6,-4));
                    Line(P(-6,-4),P(6,2)); Line(P(6,2),P(6,-4)); Line(P(6,-4),P(0,-1)); Line(P(0,-7),P(0,-14)); break;
                case CreatureShape.ButlerBeesEnemyAI:
                    Oval(0,-3,5,11); Oval(-9,6,6,4); Oval(9,6,6,4);
                    Line(P(-4,-12),P(0,-20)); Line(P(0,-20),P(4,-12)); Line(P(-4,-2),P(4,-2)); break;
                case CreatureShape.StingrayAI:
                    Line(P(0,14),P(-18,-2)); Line(P(-18,-2),P(0,-9)); Line(P(0,-9),P(18,-2)); Line(P(18,-2),P(0,14));
                    Curve(P(0,-9),P(16,-21),P(-2,-19)); break;
                case CreatureShape.ForestGiantAI:
                    Oval(0,9,8,9); Line(P(-7,1),P(-13,-17)); Line(P(7,1),P(13,-17));
                    Line(P(-13,-17),P(-3,-13)); Line(P(-3,-13),P(0,-2)); Line(P(0,-2),P(3,-13)); Line(P(3,-13),P(13,-17));
                    Line(P(-10,-4),P(-18,-8)); Line(P(10,-4),P(18,-8)); break;
                case CreatureShape.BaboonBirdAI:
                    Oval(-3,5,10,11); Line(P(5,11),P(18,4)); Line(P(18,4),P(5,0));
                    Line(P(-9,14),P(-5,21)); Line(P(-5,21),P(0,15)); Curve(P(-12,-3),P(0,-23),P(10,-7)); break;
                case CreatureShape.RedLocustBees:
                    Oval(0,-2,7,12); Oval(-11,7,6,5); Oval(11,7,6,5);
                    Line(P(-6,1),P(6,1)); Line(P(-6,-5),P(6,-5)); Line(P(-3,10),P(-8,17)); Line(P(3,10),P(8,17)); break;
                case CreatureShape.RadMechAI:
                    Box(-8,9,16,10); Box(-13,-6,26,13); Line(P(-13,2),P(-19,-8)); Line(P(13,2),P(19,-8));
                    Line(P(-7,-6),P(-10,-19),4); Line(P(7,-6),P(10,-19),4); Line(P(-5,14),P(5,14)); break;
                case CreatureShape.PumaAI:
                    Oval(0,0,12,13); Line(P(-11,5),P(-14,19)); Line(P(-14,19),P(-4,12));
                    Line(P(11,5),P(14,19)); Line(P(14,19),P(4,12)); Line(P(-6,2),P(-2,0)); Line(P(2,0),P(6,2));
                    Line(P(-3,-6),P(3,-6)); break;
                case CreatureShape.GiantKiwiAI:
                    Oval(-3,-2,12,13); Oval(6,11,6,6); Line(P(11,12),P(21,6)); Line(P(21,6),P(10,8));
                    Line(P(-8,-13),P(-10,-20)); Line(P(3,-14),P(6,-20)); break;
                case CreatureShape.FlowerSnakeEnemy:
                    Oval(0,8,4,4); Oval(0,16,4,5); Oval(-8,8,5,4); Oval(8,8,5,4);
                    Curve(P(0,3),P(-16,-17),P(8,-17)); Curve(P(8,-17),P(16,-16),P(13,-10)); break;
                case CreatureShape.DoublewingAI:
                    Line(P(0,-12),P(0,12)); Line(P(0,6),P(-17,16)); Line(P(-17,16),P(-12,0));
                    Line(P(0,6),P(17,16)); Line(P(17,16),P(12,0)); Line(P(0,-4),P(-17,-1)); Line(P(-17,-1),P(-10,-14));
                    Line(P(0,-4),P(17,-1)); Line(P(17,-1),P(10,-14)); break;
                case CreatureShape.DocileLocustBeesAI:
                    Oval(0,0,4,13); Line(P(-3,6),P(-16,12)); Line(P(3,6),P(16,12));
                    Line(P(-3,-2),P(-13,-8)); Line(P(-13,-8),P(-7,-16)); Line(P(3,-2),P(13,-8)); Line(P(13,-8),P(7,-16)); break;
                case CreatureShape.DepositItemsDesk:
                    Box(-16,-16,32,12); Curve(P(-10,-4),P(-20,21),P(-3,14));
                    Curve(P(-3,14),P(7,4),P(1,-4)); Curve(P(10,-4),P(22,15),P(7,20)); break;
                case CreatureShape.SnowmanSimpleAI:
                    Oval(0,-7,12,12); Oval(0,11,7,7); Line(P(-11,16),P(11,16));
                    Line(P(12,-5),P(20,4)); Line(P(-12,-5),P(-20,4)); Line(P(0,11),P(9,8)); break;
            }
            glyphScale=0.68f; offset=9;
        }
        if(cue==Cue.SpringRetract)
        { Line(P(-15,10),P(7,10)); Line(P(-15,-14),P(7,-14));
          for(int y=-11;y<8;y+=5) Curve(P(-11,y),P(11,y+2.5f),P(-11,y+5),2.2f);
          Line(P(16,14),P(16,-9)); Line(P(11,-3),P(16,-9)); Line(P(16,-9),P(21,-3)); }
        else if(cue==Cue.NutcrackerKick)
        { Line(P(-13,15),P(-6,-4),5); Line(P(-6,-4),P(10,-4),5); Line(P(10,-4),P(14,-11),4);
          Line(P(14,-11),P(-7,-11),4); Line(P(16,2),P(22,6)); Line(P(17,-3),P(23,-3)); Line(P(16,-15),P(21,-19)); }
        else if(cue==Cue.NutcrackerFall)
        { Line(P(-18,-16),P(18,-16)); Box(-12,-11,19,10); Oval(12,-5,5,5);
          Line(P(-15,9),P(-6,1)); Line(P(0,14),P(0,4)); Line(P(15,12),P(10,4)); }
        else if(cue==Cue.SlimeHit)
        { Curve(P(-17,-12),P(-13,14),P(0,7)); Curve(P(0,7),P(9,18),P(17,-12));
          Curve(P(17,-12),P(0,-18),P(-17,-12)); Line(P(1,3),P(7,-3)); Line(P(7,3),P(1,-3));
          Line(P(-17,13),P(-12,8)); Line(P(4,20),P(4,14)); Line(P(15,12),P(21,17)); }
        else if(cue is Cue.NeckSnap or Cue.OtherNeckSnap)
        { Oval(-6,10,7,8); Line(P(-10,9),P(-7,8)); Line(P(-3,4),P(3,0));
          Line(P(4,-7),P(7,-17),4); Curve(P(-15,-19),P(4,-9),P(18,-19));
          Line(P(-2,-5),P(7,-1)); Line(P(7,-1),P(2,-8)); Line(P(2,-8),P(11,-4));
          Line(P(11,8),P(17,12)); Line(P(13,3),P(21,3)); }
        else if(cue==Cue.CruiserHood)
        { Box(-15,-8,30,10); Oval(-9,-12,3,3); Oval(9,-12,3,3);
          Line(P(-14,4),P(-3,19)); Line(P(-3,19),P(13,11)); Line(P(0,6),P(0,15)); Line(P(-4,11),P(0,15)); }
        else if(cue==Cue.CruiserDoor)
        { Box(-15,-15,17,28); Line(P(2,13),P(16,7)); Line(P(16,7),P(16,-18)); Line(P(16,-18),P(2,-15));
          Line(P(-11,8),P(-3,8)); Line(P(10,-3),P(13,-3)); }
        else if(cue==Cue.CruiserRearDoor)
        { Box(-16,-15,32,30); Line(P(-15,7),P(15,7)); Line(P(-15,1),P(15,1));
          Line(P(-15,-5),P(15,-5)); Line(P(-5,18),P(0,23)); Line(P(0,23),P(5,18)); Line(P(0,13),P(0,23)); }
        else if(cue is Cue.CruiserIgnition or Cue.CruiserStarted)
        { Box(-12,-9,24,18); Line(P(-6,13),P(6,13)); Line(P(-17,-4),P(-17,4));
          if(cue==Cue.CruiserIgnition) { Curve(P(-13,-16),P(15,-23),P(17,-7)); Line(P(17,-7),P(10,-10)); Line(P(17,-7),P(20,-14)); }
          else { Line(P(-6,0),P(-1,-5),3); Line(P(-1,-5),P(7,5),3); Line(P(16,8),P(21,14)); Line(P(-16,8),P(-21,14)); } }
        else if(cue==Cue.CruiserKey)
        { Oval(-7,8,7,7); Line(P(-2,3),P(13,-12),4); Line(P(7,-6),P(12,-1)); Line(P(12,-11),P(17,-6)); }
        else if(birdTracks)
        {
            // Alternating three-toed tracks, separated from human shoe silhouettes.
            for(int i=0;i<2;i++)
            {
                float x=i==0 ? -9 : 9, y=i==0 ? -8 : 4;
                Line(P(x,y-7),P(x,y+9),3.5f);
                Line(P(x,y),P(x-7,y+6),3.5f);
                Line(P(x,y),P(x+7,y+6),3.5f);
                Oval(x,y,2,2,40,true);
            }
        }
        else if(cue==Cue.WolfFootsteps)
        {
            // Solid pads and four separated toes stay legible at small HUD sizes.
            for(int i=0;i<2;i++)
            {
                float x=i==0 ? -10 : 10, y=i==0 ? -13 : 0;
                Oval(x,y,4,2.8f,40,true);
                Oval(x-7.5f,y+6,1,1.7f,40,true);
                Oval(x-2.6f,y+10,1,1.8f,40,true);
                Oval(x+2.6f,y+10,1,1.8f,40,true);
                Oval(x+7.5f,y+6,1,1.7f,40,true);
            }
        }
        else if(cue is Cue.MeteorApproach or Cue.MeteorImpact)
        {
            Oval(-4,-3,8,8); Oval(-6,0,2,2);
            if(cue==Cue.MeteorApproach)
            { Line(P(-6,9),P(7,20)); Line(P(2,8),P(17,20)); Line(P(7,1),P(20,13)); }
            else
            { Line(P(-19,-15),P(19,-15)); Line(P(-14,-7),P(-20,-1)); Line(P(7,-6),P(17,1)); Line(P(7,5),P(12,14)); }
        }
        else if(cue==Cue.PlantClear)
        {
            Curve(P(-9,-7),P(-19,10),P(2,16)); Curve(P(2,16),P(8,-2),P(-9,-7));
            Line(P(-10,-16),P(-6,-7)); Line(P(-3,-2),P(3,9));
            Line(P(7,-8),P(18,3),3); Line(P(7,3),P(18,-8),3);
        }
        else if(cue==Cue.BloomChestOpen)
        {
            Curve(P(-3,16),P(-23,1),P(-5,-17)); Curve(P(3,16),P(23,1),P(5,-17));
            for(int y=-9;y<=9;y+=6) { Line(P(-5,y),P(-12,y+3)); Line(P(5,y),P(12,y+3)); }
            Line(P(0,13),P(3,5)); Line(P(3,5),P(-3,-3)); Line(P(-3,-3),P(0,-13));
        }
        else if(cue is Cue.WolfTongue or Cue.WolfPull)
        {
            Curve(P(-17,12),P(-4,17),P(-7,4)); Curve(P(-17,-1),P(-7,-6),P(-7,4));
            Curve(P(-7,4),P(18,14),P(16,-5),4); Curve(P(16,-5),P(14,-13),P(5,-9),4);
            if(cue==Cue.WolfPull) { Line(P(5,-16),P(-9,-16)); Line(P(-3,-11),P(-9,-16)); Line(P(-3,-21),P(-9,-16)); }
            else { Line(P(14,9),P(22,13)); Line(P(18,3),P(23,3)); }
        }
        else if(cue is Cue.WolfCall or Cue.WolfGrowl or Cue.WolfSnarl or Cue.WolfAttack or Cue.WolfHit or Cue.WolfDeath)
        {
            Line(P(-12,3),P(-14,17)); Line(P(-14,17),P(-4,10)); Line(P(-4,10),P(4,10));
            Line(P(4,10),P(14,17)); Line(P(14,17),P(12,3));
            Line(P(-12,3),P(-8,-8)); Line(P(-8,-8),P(0,-15)); Line(P(0,-15),P(8,-8)); Line(P(8,-8),P(12,3));
            if(cue==Cue.WolfDeath)
            { Line(P(-8,5),P(-3,0)); Line(P(-8,0),P(-3,5)); Line(P(3,5),P(8,0)); Line(P(3,0),P(8,5)); }
            else { Line(P(-8,4),P(-4,2)); Line(P(4,2),P(8,4)); }
            if(cue is Cue.WolfAttack or Cue.WolfSnarl)
            { Line(P(-5,-3),P(-3,-9)); Line(P(-3,-9),P(0,-4)); Line(P(0,-4),P(3,-9)); Line(P(3,-9),P(5,-3)); }
            else { Line(P(-3,-5),P(3,-5)); }
            if(cue==Cue.WolfCall) { Curve(P(17,6),P(24,0),P(17,-6)); }
            else if(cue==Cue.WolfGrowl) { Line(P(-16,-13),P(-11,-17)); Line(P(11,-17),P(16,-13)); }
            else if(cue==Cue.WolfSnarl) { Line(P(-20,0),P(-16,-4)); Line(P(16,-4),P(20,0)); }
            else if(cue==Cue.WolfAttack) { Line(P(-19,-11),P(-13,-20)); Line(P(-14,-11),P(-8,-20)); }
            else if(cue==Cue.WolfHit) { Line(P(14,-11),P(21,-8)); Line(P(16,-15),P(22,-18)); }
        }
        else if(cue==Cue.Spring)
        { Line(P(-12,17),P(12,17)); Line(P(-12,-17),P(12,-17));
          for(int y=-14;y<14;y+=7) { Curve(P(-8,y),P(20,y+4),P(-8,y+7),2.5f); } }
        else if(cue==Cue.DiscoMusic)
        { Line(P(0,20),P(0,14)); Oval(0,0,14,14); Oval(0,0,6,14); Line(P(-13,5),P(13,5)); Line(P(-13,-5),P(13,-5)); Line(P(-20,12),P(-16,9)); Line(P(20,12),P(16,9)); }
        else if(cue==Cue.CarHood)
        { Box(-14,-7,28,12); Oval(-8,-11,3,3); Oval(8,-11,3,3); Line(P(-13,6),P(-3,18)); Line(P(-3,18),P(13,9)); }
        else if(cue==Cue.CabinetDoor)
        { Box(-15,-17,30,34); Line(P(0,-17),P(0,17)); Line(P(-5,-2),P(-5,4)); Line(P(5,-2),P(5,4)); Line(P(-15,-10),P(15,-10)); }
        else if(cue is Cue.ShipTravel or Cue.ShipArrival)
        { Oval(0,0,12,12); Curve(P(-7,9),P(5,1),P(-5,-10));
          if(cue==Cue.ShipTravel) { Line(P(-19,16),P(-4,16)); Line(P(-8,20),P(-4,16)); Line(P(4,-16),P(19,-16)); Line(P(15,-20),P(19,-16)); }
          else { Line(P(7,-11),P(12,-16),4); Line(P(12,-16),P(21,-6),4); } }
        else if(cue==Cue.PlantGrowth)
        { Line(P(0,-16),P(0,6)); Curve(P(0,0),P(-19,3),P(-12,13)); Curve(P(-12,13),P(1,15),P(0,0)); Curve(P(0,5),P(1,20),P(13,15)); Curve(P(13,15),P(15,5),P(0,5)); Line(P(-10,-16),P(10,-16)); }
        else if(cue==Cue.SporeCloud)
        { Oval(-8,3,7,5); Oval(1,9,7,6); Oval(10,2,6,5); Oval(-7,-9,2,2); Oval(2,-12,2,2); Oval(12,-8,2,2); }
        else if(cue==Cue.BloomTreatment)
        { Curve(P(-11,-11),P(-20,9),P(1,14)); Curve(P(1,14),P(4,-8),P(-11,-11)); Line(P(-12,-12),P(-4,6)); Line(P(7,-3),P(19,-3),4); Line(P(13,-9),P(13,3),4); }
        else if(cue==Cue.BloomBreath)
        { Curve(P(-2,13),P(-24,-2),P(-7,-12)); Curve(P(-7,-12),P(0,-13),P(-2,13)); Curve(P(2,13),P(24,-2),P(7,-12)); Curve(P(7,-12),P(0,-13),P(2,13)); Line(P(0,17),P(0,8)); }
        else if(cue is Cue.BloomBurst or Cue.BloomChomp or Cue.BloomRoar)
        { Oval(0,0,9,11); for(int j=0;j<6;j++) { float a=j*Mathf.PI/3; Line(P(13*Mathf.Cos(a),13*Mathf.Sin(a)),P(19*Mathf.Cos(a),19*Mathf.Sin(a))); }
          if(cue==Cue.BloomBurst) { Line(P(-5,6),P(1,1)); Line(P(1,1),P(-3,-6)); Line(P(-3,-6),P(6,-2)); }
          else if(cue==Cue.BloomChomp) { for(int x=-4;x<=4;x+=4) { Line(P(x-2,7),P(x,1)); Line(P(x,1),P(x+2,7)); Line(P(x,-7),P(x,-3)); } }
          else { Oval(0,0,3,6); } }
        else if(cue is Cue.CreatureAttack or Cue.Bite)
        { Curve(P(-15,7),P(0,20),P(15,7)); Curve(P(-15,-7),P(0,-20),P(15,-7));
          for(int x=-9;x<=9;x+=9) { Line(P(x-3,10),P(x,2)); Line(P(x,2),P(x+3,10)); Line(P(x-3,-10),P(x,-2)); Line(P(x,-2),P(x+3,-10)); } }
        else if(cue==Cue.CreatureDeath)
        { Oval(0,3,13,12); for(int x=-6;x<=6;x+=12) { Line(P(x-2,7),P(x+2,3)); Line(P(x+2,7),P(x-2,3)); } Curve(P(-5,-4),P(0,-1),P(5,-4)); Line(P(-15,-14),P(15,-14)); }
        else if(cue is Cue.BreakerDoor or Cue.BreakerSwitch)
        { Box(-14,-15,28,30); if(cue==Cue.BreakerDoor) { Line(P(-7,15),P(7,10)); Line(P(7,10),P(7,-10)); Line(P(7,-10),P(-7,-15)); }
          else { Box(-8,-9,16,18); Line(P(-4,-2),P(5,7),4); Oval(5,7,2,2,40,true); } }
        else if(cue is Cue.ElevatorMusic)
        { Box(-15,-15,30,30); Line(P(-10,10),P(-10,-9)); Oval(0,-6,4,3,40,true); Line(P(4,-6),P(4,9)); Line(P(4,9),P(10,7)); }
        else if(cue==Cue.VehicleJump)
        { Box(-13,-4,26,11); Oval(-8,-9,3,3); Oval(8,-9,3,3); Line(P(-14,-17),P(14,-17)); Line(P(0,9),P(0,20)); Line(P(-4,16),P(0,20)); Line(P(0,20),P(4,16)); }
        else if(cue is Cue.TzpEmpty or Cue.TzpRelease)
        { Box(-7,-13,14,24); Line(P(-4,14),P(4,14));
          if(cue==Cue.TzpEmpty) { Line(P(-4,-5),P(4,3)); Line(P(-4,3),P(4,-5)); }
          else { Line(P(11,0),P(20,0)); Line(P(16,4),P(20,0)); Line(P(20,0),P(16,-4)); } }
        else if(cue==Cue.FurniturePlace)
        { Box(-12,-8,24,9); Line(P(-12,1),P(-12,8)); Line(P(-12,8),P(12,8)); Line(P(12,8),P(12,1)); Line(P(-9,-8),P(-9,-14)); Line(P(9,-8),P(9,-14)); Line(P(0,20),P(0,11)); Line(P(-4,15),P(0,11)); Line(P(0,11),P(4,15)); }
        else if(cue==Cue.Fridge)
        { Box(-12,-17,24,34); Line(P(-12,4),P(12,4)); Line(P(-7,8),P(-7,12)); Line(P(-7,-3),P(-7,-9)); }
        else if(cue==Cue.MicrowaveDoor)
        { Box(-17,-12,34,24); Line(P(7,-12),P(7,12)); Line(P(-13,8),P(1,4)); Line(P(1,4),P(1,-8)); Line(P(1,-8),P(-13,-12)); Oval(12,4,1.5f,1.5f,40,true); }
        else if(cue==Cue.ChairShock)
        { Line(P(-12,14),P(-12,-7),4); Line(P(-12,-7),P(8,-7),4); Line(P(-9,-7),P(-9,-16)); Line(P(6,-7),P(6,-16)); Line(P(9,17),P(1,5)); Line(P(1,5),P(12,5)); Line(P(12,5),P(5,-3)); }
        else if(cue==Cue.ZedDog)
        { Curve(P(-10,11),P(-23,10),P(-15,-3),4); Curve(P(10,11),P(23,10),P(15,-3),4); Oval(0,0,12,13); Oval(-5,5,1.5f,1.5f,40,true); Oval(5,5,1.5f,1.5f,40,true); Oval(0,-1,3,2,40,true); Curve(P(-5,-5),P(0,-10),P(5,-5)); }
        else if(cue==Cue.Drowning)
        { Wave(-14,-8); Oval(0,5,7,7); Oval(12,13,2,2); }
        else if(cue==Cue.MudSink)
        { Wave(-14,-13); Line(P(-8,14),P(-3,-5),5); Line(P(-3,-5),P(6,-5),5); Line(P(12,12),P(12,-2)); Line(P(8,2),P(12,-2)); Line(P(12,-2),P(16,2)); }
        else if(cue==Cue.ShipLanding)
        {
            // Front-facing ship with landing struts and an unambiguous descent arrow.
            Line(P(-12,3),P(12,3)); Line(P(12,3),P(16,-7));
            Line(P(16,-7),P(10,-11)); Line(P(10,-11),P(-10,-11));
            Line(P(-10,-11),P(-16,-7)); Line(P(-16,-7),P(-12,3));
            Line(P(-7,-2),P(7,-2),2.5f);
            Line(P(-11,-10),P(-15,-16)); Line(P(11,-10),P(15,-16));
            Line(P(-20,-16),P(-10,-16)); Line(P(10,-16),P(20,-16));
            Line(P(0,20),P(0,9)); Line(P(-5,14),P(0,9)); Line(P(0,9),P(5,14));
        }
        else if(cue==Cue.ShipTakeoff)
        { Box(-15,-3,23,12); Line(P(8,-3),P(16,-3)); Line(P(16,-3),P(16,5)); Line(P(16,5),P(8,9)); Line(P(-18,-14),P(18,-14));
          float sign=cue==Cue.ShipLanding ? -1 : 1; Line(P(0,11),P(0,21)); Line(P(-4,16-sign*5),P(0,16+sign*5)); Line(P(0,16+sign*5),P(4,16-sign*5)); }
        else if(cue==Cue.ItemDrop)
        { Box(-8,-6,16,13); Line(P(0,20),P(0,10)); Line(P(-4,14),P(0,10)); Line(P(0,10),P(4,14)); Line(P(-16,-14),P(16,-14)); Line(P(-12,-8),P(-16,-4)); Line(P(12,-8),P(16,-4)); }
        else if(cue==Cue.CanShake)
        { Box(-8,-13,16,24); Line(P(-4,14),P(4,14)); Line(P(-17,-7),P(-17,8)); Line(P(-20,4),P(-17,8)); Line(P(17,7),P(17,-8)); Line(P(14,-4),P(17,-8)); }
        else if(cue==Cue.ToyTrain)
        { Box(-15,-6,29,15); Box(-11,9,12,7); Line(P(8,9),P(8,16),5); Oval(-9,-11,4,4); Oval(9,-11,4,4); }
        else if(cue==Cue.LaserSwitch)
        { Line(P(-15,-8),P(-3,-2),6); Line(P(-1,0),P(18,10),2); Oval(20,11,2,2,40,true); }
        else if(cue is Cue.KnifeAttack)
        { Line(P(-12,-13),P(-3,-4),5); Line(P(-6,-1),P(0,-7),3); Line(P(-3,-4),P(12,15)); Line(P(12,15),P(10,1)); Line(P(10,1),P(-3,-4)); }
        else if(cue==Cue.SuitChange)
        { Line(P(-6,13),P(-16,8)); Line(P(-16,8),P(-12,-1)); Line(P(-12,-1),P(-8,2)); Line(P(-8,2),P(-8,-14)); Line(P(-8,-14),P(8,-14)); Line(P(8,-14),P(8,2)); Line(P(8,2),P(12,-1)); Line(P(12,-1),P(16,8)); Line(P(16,8),P(6,13)); Curve(P(-6,13),P(0,2),P(6,13)); }
        else if(cue==Cue.VehicleBoost)
        { Box(-6,-5,24,12); Line(P(-2,7),P(2,14)); Line(P(2,14),P(12,14)); Line(P(12,14),P(16,7)); Oval(0,-9,3,3); Oval(13,-9,3,3);
          Line(P(-11,4),P(-20,8)); Line(P(-11,0),P(-22,0)); Line(P(-11,-4),P(-20,-8)); }
        else if(cue==Cue.VehicleRefuel)
        { Line(P(-17,12),P(-8,19)); Line(P(-8,19),P(1,9)); Line(P(1,9),P(-8,2)); Line(P(-8,2),P(-17,12));
          Line(P(1,9),P(6,6)); Oval(8,1,2,3,40,true); Box(-4,-15,20,7); Line(P(-4,-8),P(3,-4)); Line(P(3,-4),P(12,-4)); Line(P(12,-4),P(16,-8)); }
        else if(cue==Cue.SeatEject)
        { Line(P(-11,6),P(-11,-9),4); Line(P(-11,-9),P(9,-9),4); Line(P(-8,-10),P(-8,-16)); Line(P(8,-10),P(8,-16)); Line(P(2,-3),P(2,17)); Line(P(-4,11),P(2,17)); Line(P(2,17),P(8,11)); }
        else if(cue is Cue.BodyCrush or Cue.DeathSound or Cue.CreatureHit)
        { Oval(0,6,5,5); Curve(P(-11,-12),P(0,2),P(11,-12));
          if(cue==Cue.BodyCrush) { Line(P(-15,17),P(15,17),4); Line(P(-15,-16),P(15,-16),4); }
          else { Line(P(11,12),P(16,17)); Line(P(13,6),P(20,6)); if(cue==Cue.DeathSound) Line(P(-10,-15),P(10,-15),4); }
        }
        else if(cue==Cue.Fan)
        { Oval(0,0,3,3); for(int j=0;j<3;j++) { float a=j*2*Mathf.PI/3; Curve(P(3*Mathf.Cos(a),3*Mathf.Sin(a)),P(24*Mathf.Cos(a+0.4f),24*Mathf.Sin(a+0.4f)),P(8*Mathf.Cos(a+1),8*Mathf.Sin(a+1)),5); } }
        else if(cue==Cue.Web)
        { for(int j=0;j<6;j++) { float a=j*Mathf.PI/3; Line(P(0,0),P(17*Mathf.Cos(a),17*Mathf.Sin(a)),2); } Oval(0,0,7,7); Oval(0,0,13,13); }
        else if(cue is Cue.Steam or Cue.Spray)
        { Line(P(-17,-9),P(-17,6)); Line(P(-17,6),P(-8,6)); for(int j=0;j<3;j++) Curve(P(-5,7-j*7),P(5,14-j*7),P(18,7-j*7),2.5f); }
        else if(cue is Cue.PressureDoor or Cue.ShutterDoor or Cue.ShipDoor)
        { Box(-15,-15,30,30); if(cue==Cue.ShutterDoor) { for(int y=-9;y<14;y+=6) Line(P(-11,y),P(11,y)); } else { Line(P(0,-15),P(0,15)); Line(P(-10,0),P(-4,0)); Line(P(4,0),P(10,0)); if(cue==Cue.PressureDoor) { Line(P(-7,4),P(-11,0)); Line(P(-11,0),P(-7,-4)); Line(P(7,4),P(11,0)); Line(P(11,0),P(7,-4)); } else Oval(0,7,4,4); } }
        else if(cue is Cue.MineBeep or Cue.MinePress)
        { Oval(0,-7,16,5); Curve(P(-13,-5),P(0,13),P(13,-5)); Line(P(0,17),P(0,9)); if(cue==Cue.MinePress) { Line(P(-4,13),P(0,9)); Line(P(0,9),P(4,13)); } else { Line(P(-10,15),P(-13,18)); Line(P(10,15),P(13,18)); } }
        else if(cue==Cue.Plushie)
        { Oval(-7,14,3,3); Oval(7,14,3,3); Oval(0,6,9,8); Oval(-3,8,1,1,40,true); Oval(3,8,1,1,40,true); Oval(0,3,2,1.5f,40,true); Oval(0,-8,6,7); Oval(-7,-14,3,2); Oval(7,-14,3,2);
          Line(P(-20,-2),P(-12,-2)); Line(P(-16,2),P(-12,-2)); Line(P(-12,-2),P(-16,-6)); Line(P(20,-2),P(12,-2)); Line(P(16,2),P(12,-2)); Line(P(12,-2),P(16,-6)); }
        else if(cue==Cue.RecordPlayer)
        { Box(-16,-14,32,28); Oval(-3,0,10,10); Oval(-3,0,2,2); Line(P(12,10),P(12,0)); Line(P(12,0),P(5,-6)); }
        else if(cue==Cue.Toilet)
        { Box(-13,1,11,14); Oval(4,-1,12,4); Curve(P(-6,-3),P(-6,-14),P(8,-13)); Line(P(8,-13),P(9,-4)); }
        else if(cue==Cue.Candle)
        { Box(-5,-14,10,17); Curve(P(0,17),P(-10,5),P(0,6)); Curve(P(0,6),P(9,7),P(0,17)); Line(P(-11,-16),P(11,-16)); }
        else if(cue==Cue.Shower)
        { Curve(P(-13,-12),P(-19,18),P(4,14)); Line(P(-3,9),P(10,9),4); for(int x=-1;x<=9;x+=5) { Line(P(x,3),P(x,-2),2); Line(P(x,-7),P(x,-12),2); } }
        else if(cue==Cue.Microwave)
        { Box(-17,-12,34,26); Box(-13,-8,21,18); Oval(13,6,1.5f,1.5f,40,true); Wave(-12,0); }
        else if(cue==Cue.Pumpkin)
        { Oval(0,-1,16,12); Oval(0,-1,7,12); Line(P(0,11),P(4,17),4); Line(P(-9,-1),P(-6,-1)); Line(P(6,-1),P(9,-1)); }
        else if(cue==Cue.WaterSplash)
        { Curve(P(-17,-6),P(0,-17),P(17,-6)); Line(P(-11,2),P(-15,9)); Line(P(11,2),P(15,9)); Line(P(0,3),P(0,14)); }
        else if(cue is Cue.GroundRadioVoice or Cue.RadioSignal or Cue.RadioRelay)
        {
            Box(-10,-14,17,23); Line(P(-6,9),P(-6,18));
            Box(-6,0,9,5); for(int y=-5;y>=-10;y-=5) Line(P(-5,y),P(2,y),2);
            Curve(P(11,11),P(18,5),P(11,-1));
        }
        else if(cue is Cue.Teleport or Cue.InverseTeleport)
        {
            Oval(0,-12,15,4); Oval(0,12,15,4);
            float flip=cue==Cue.InverseTeleport ? -1 : 1;
            Line(P(0,-6*flip),P(0,6*flip)); Line(P(-5,flip),P(0,6*flip)); Line(P(0,6*flip),P(5,flip));
            Line(P(-14,-6),P(-14,6),2); Line(P(14,-6),P(14,6),2);
        }
        else if(cue==Cue.LightSwitch)
        {
            Curve(P(-6,-5),P(-19,13),P(0,14)); Curve(P(0,14),P(19,13),P(6,-5));
            Line(P(-6,-5),P(6,-5)); Line(P(-5,-10),P(5,-10)); Line(P(-3,-14),P(3,-14));
            Line(P(-17,12),P(-21,15),2); Line(P(17,12),P(21,15),2);
        }
        else if(cue is Cue.RadarPing or Cue.RadarSwitch)
        {
            Oval(0,0,15,15); Oval(0,0,7,7);
            Line(P(0,0),P(10,11)); Oval(-9,-6,2,2,40,true);
        }
        else if(cue==Cue.WorldHeartbeat)
        {
            // A pulsing physical item, deliberately not a personal ECG/heart icon.
            Oval(0,0,7,11); Curve(P(-13,-9),P(-21,0),P(-13,9)); Curve(P(13,-9),P(21,0),P(13,9));
        }
        else if(cue==Cue.Environment)
        { Wave(-15,9); Wave(-15,0); Wave(-15,-9); }
        else if(cue==Cue.Television)
        {
            Box(-16,-9,32,22); Line(P(-8,-14),P(8,-14)); Line(P(0,-9),P(0,-14));
            Line(P(-4,-3),P(-4,7)); Line(P(-4,7),P(6,2)); Line(P(6,2),P(-4,-3));
        }
        else if(cue is Cue.Explosion or Cue.Burst)
        {
            for(int j=0;j<8;j++) {
                float a=j*Mathf.PI/4,b=a+Mathf.PI/8,c=a+Mathf.PI/4;
                Line(P(Mathf.Cos(a)*17,Mathf.Sin(a)*17),P(Mathf.Cos(b)*8,Mathf.Sin(b)*8));
                Line(P(Mathf.Cos(b)*8,Mathf.Sin(b)*8),P(Mathf.Cos(c)*17,Mathf.Sin(c)*17));
            }
        }
        else if(cue is Cue.AmbientFire or Cue.Fire or Cue.VehicleFire)
        {
            Curve(P(0,17),P(-2,2),P(-9,4)); Curve(P(-9,4),P(-18,-15),P(0,-15));
            Curve(P(0,-15),P(20,-13),P(10,7)); Curve(P(10,7),P(6,0),P(0,17));
        }
        else if(cue==Cue.Charging || cue==Cue.BatteryWarning)
        {
            Box(-15,-9,27,18); Line(P(15,-4),P(15,4),4);
            if(cue==Cue.Charging) { Line(P(1,6),P(-5,-1)); Line(P(-5,-1),P(2,-1)); Line(P(2,-1),P(-2,-6)); }
            else { Line(P(-2,5),P(-2,-1)); Oval(-2,-5,1,1,40,true); }
        }
        else if(cue==Cue.PowerHum)
        {
            Box(-10,-5,20,15); Line(P(-6,10),P(-6,17)); Line(P(6,10),P(6,17));
            Curve(P(0,-5),P(-8,-17),P(10,-14));
        }
        else if(cue==Cue.Apparatus)
        { Box(-10,-12,20,24); Box(-5,-6,10,12); Line(P(-15,7),P(-10,7)); Line(P(10,-7),P(15,-7)); }
        else if(cue==Cue.ToyRobot)
        { Box(-11,-11,22,24); Line(P(0,13),P(0,18)); Oval(-5,5,2,2,40,true); Oval(5,5,2,2,40,true); Line(P(-5,-5),P(5,-5)); Line(P(-16,-6),P(-16,6)); Line(P(16,-6),P(16,6)); }
        else if(cue==Cue.Remote)
        { Box(-8,-16,16,30); Oval(0,8,2,2,40,true); for(int y=0;y>=-10;y-=5) { Line(P(-4,y),P(-3,y),2); Line(P(3,y),P(4,y),2); } }
        else if(cue==Cue.WeedSpray)
        { Curve(P(0,-12),P(-19,9),P(12,13)); Curve(P(12,13),P(18,-6),P(0,-12)); Line(P(-3,-16),P(9,9)); Line(P(-7,7),P(4,0),2); }
        else if(cue==Cue.ItemNoise)
        {
            // Generic audible object: compact solid silhouette, balanced waves.
            Line(P(-10,8),P(0,13)); Line(P(0,13),P(10,8));
            Line(P(-10,8),P(0,3)); Line(P(0,3),P(10,8));
            Line(P(-10,8),P(-10,-9)); Line(P(-10,-9),P(0,-14));
            Line(P(0,-14),P(10,-9)); Line(P(10,-9),P(10,8)); Line(P(0,3),P(0,-14));
            Curve(P(-16,7),P(-21,0),P(-16,-7));
            Curve(P(16,7),P(21,0),P(16,-7));
        }
        else if(cue==Cue.Elevator)
        {
            // Cabin on rails, with separate up/down motion arrows.
            Line(P(-11,-17),P(-11,17)); Line(P(11,-17),P(11,17));
            Box(-11,-11,22,23); Line(P(-10,6),P(10,6)); Line(P(0,6),P(0,-11));
            Line(P(-19,-8),P(-19,10)); Line(P(-23,6),P(-19,10)); Line(P(-19,10),P(-15,6));
            Line(P(19,8),P(19,-10)); Line(P(15,-6),P(19,-10)); Line(P(19,-10),P(23,-6));
        }
        else if(cue==Cue.Cabinet)
        { Box(-14,2,28,14); Line(P(-14,2),P(-18,-10)); Line(P(14,2),P(18,-10)); Box(-18,-17,36,7); Line(P(-18,-10),P(18,-10)); Line(P(-5,-13),P(5,-13)); Line(P(-5,9),P(5,9)); }
        else if(cue is Cue.Gunshot or Cue.GunClick or Cue.Reload or Cue.Aim or Cue.Ricochet)
        {
            Box(-14,0,26,9); Line(P(-12,0),P(-12,-12),5); Line(P(-12,-12),P(-5,-12)); Line(P(-5,-12),P(-3,0));
            if(cue==Cue.Gunshot || cue==Cue.Ricochet) { Line(P(16,8),P(21,12)); Line(P(16,3),P(22,3)); }
            if(cue==Cue.Reload) { Line(P(9,-14),P(9,-3)); Line(P(5,-7),P(9,-3)); Line(P(9,-3),P(13,-7)); }
            if(cue==Cue.Aim) { Line(P(1,12),P(1,17),2); Line(P(-3,15),P(5,15),2); }
        }
        else if(cue is Cue.Growl or Cue.Roar or Cue.Howl or Cue.Screaming or Cue.Crying or Cue.Cackle or Cue.Laughter)
        {
            Oval(-3,0,9,13); Line(P(-8,6),P(2,6)); Line(P(-8,-6),P(2,-6));
            Curve(P(10,-6),P(17,0),P(10,6));
            if(cue is Cue.Roar or Cue.Screaming or Cue.Howl) Curve(P(15,-11),P(25,0),P(15,11));
        }
        else if(cue == Cue.Voice)
        {
            Oval(0,3,15,10); Line(P(-9,-5),P(-12,-13)); Line(P(-12,-13),P(-1,-7));
            for(int i=0;i<3;i++) Line(P(-7+i*7,3),P(-6+i*7,3),2.5f);
        }
        else if(cue is Cue.StaticWarning or Cue.Thunder or Cue.Zap or Cue.PowerHum or Cue.Charging)
        {
            Line(P(3,17),P(-10,-1)); Line(P(-10,-1),P(3,-1)); Line(P(3,-1),P(-3,-16));
            Line(P(-3,-16),P(13,5)); Line(P(13,5),P(2,5)); Line(P(2,5),P(3,17));
        }
        else if(cue is Cue.BridgeCreak or Cue.BridgeCollapse)
        {
            Line(P(-16,-13),P(-16,12)); Line(P(16,-13),P(16,12));
            Curve(P(-16,12),P(0,-3),P(16,12));
            Line(P(-16,-5),P(-3,-5)); Line(P(3,-5),P(16,-5));
            for(int i=-10;i<=10;i+=5) Line(P(i,3),P(i,-5));
            if(cue==Cue.BridgeCollapse) { Line(P(-3,-5),P(0,-12)); Line(P(3,-5),P(6,-11)); }
        }
        else if(cue is Cue.Horn or Cue.AirHorn or Cue.ClownHorn)
        {
            Line(P(-14,5),P(-5,5)); Line(P(-5,5),P(6,12)); Line(P(6,12),P(6,-10));
            Line(P(6,-10),P(-5,-4)); Line(P(-5,-4),P(-14,-4)); Line(P(-14,-4),P(-14,5));
            Curve(P(11,8),P(18,1),P(11,-6));
        }
        else if(cue is Cue.Ladder or Cue.LadderClimb)
        {
            Line(P(-9,-14),P(-9,15)); Line(P(9,-14),P(9,15));
            for(int i=-10;i<=12;i+=7) Line(P(-9,i),P(9,i));
        }
        else if(cue is Cue.ToolSwing)
        {
            Line(P(-11,-13),P(5,7),4); Line(P(1,7),P(7,14),5); Line(P(7,14),P(14,8),5);
            Curve(P(-12,13),P(-2,18),P(3,16),2);
        }
        else if(cue==Cue.CounterBell)
        {
            Curve(P(-15,-5),P(-13,15),P(0,15));Curve(P(0,15),P(13,15),P(15,-5));
            Line(P(-18,-6),P(18,-6));Line(P(-13,-11),P(13,-11));
            Line(P(0,15),P(0,19));Line(P(-4,19),P(4,19));
        }
        else if(cue==Cue.AccessHatch)
        {
            Line(P(-18,-13),P(11,-13));Line(P(11,-13),P(19,-3));Line(P(19,-3),P(-10,-3));Line(P(-10,-3),P(-18,-13));
            Line(P(-10,-3),P(-10,14));Line(P(-10,14),P(19,14));Line(P(19,14),P(19,-3));
            Line(P(-5,8),P(14,8));Line(P(-5,3),P(14,3));
            Line(P(-2,-7),P(4,-7));
        }
        else if(cue==Cue.WarehouseDoor)
        {
            // Two sliding leaves. Horizontal travel denotes the mechanism, not hidden open/close state.
            Line(P(-20,-7),P(-20,15));Line(P(-20,15),P(20,15));Line(P(20,15),P(20,-7));
            Line(P(-15,10),P(-5,10));Line(P(-5,10),P(-5,-6));Line(P(-5,-6),P(-15,-6));Line(P(-15,-6),P(-15,10));
            Line(P(5,10),P(15,10));Line(P(15,10),P(15,-6));Line(P(15,-6),P(5,-6));Line(P(5,-6),P(5,10));
            Line(P(-9,4),P(-9,0),2);Line(P(9,4),P(9,0),2);
            Line(P(-15,-15),P(15,-15),2.5f);
            Line(P(-10,-10),P(-15,-15),2.5f);Line(P(-15,-15),P(-10,-20),2.5f);
            Line(P(10,-10),P(15,-15),2.5f);Line(P(15,-15),P(10,-20),2.5f);
        }
        else if(cue==Cue.CounterShutter)
        {
            Line(P(-17,-14),P(-17,15));Line(P(-17,15),P(17,15));Line(P(17,15),P(17,-14));
            for(int y=10;y>=0;y-=5) Line(P(-12,y),P(12,y));
            Line(P(-21,-13),P(21,-13));Line(P(-10,-18),P(10,-18));
        }
        else if(cue==Cue.CounterAttack)
        {
            // Organic curling tentacle: broad base, curved taper and small suckers.
            Curve(P(-14,-16),P(-18,8),P(-5,10),6);
            Curve(P(-5,10),P(12,17),P(13,4),4);
            Curve(P(13,4),P(11,-4),P(5,0),2);
            Oval(-10,-6,1.5f,1.5f);Oval(-9,1,1.5f,1.5f);Oval(-4,6,1.5f,1.5f);
            Line(P(16,12),P(21,16));Line(P(18,3),P(23,3));
        }
        else if(cue==Cue.CounterGrab)
        {
            Line(P(-5,18),P(-5,2),4);Curve(P(-5,2),P(-8,-14),P(5,-14),4);
            Curve(P(5,-14),P(18,-13),P(12,-2),4);Line(P(12,-2),P(7,-5));
            Line(P(-19,-8),P(-12,-8));Line(P(-12,-8),P(-12,-17));Line(P(-12,-17),P(-19,-17));Line(P(-19,-17),P(-19,-8));
        }
        else if(cue is Cue.CounterWarning or Cue.CounterAmbience)
        {
            Line(P(-17,-15),P(-17,15));Line(P(-17,15),P(-7,15));Line(P(-17,1),P(-9,1));Line(P(-17,-15),P(-7,-15));
            Curve(P(-2,9),P(7,0),P(-2,-9));Curve(P(7,14),P(19,0),P(7,-14));
            if(cue==Cue.CounterWarning) { Line(P(-12,11),P(-7,6));Line(P(-7,6),P(-12,0));Line(P(-12,0),P(-7,-6)); }
        }
        else if(cue==Cue.CounterSnore)
        {
            Curve(P(-18,-3),P(-9,-12),P(0,-3));
            Line(P(-2,2),P(5,2));Line(P(5,2),P(-2,-5));Line(P(-2,-5),P(5,-5));
            Line(P(8,15),P(18,15));Line(P(18,15),P(8,5));Line(P(8,5),P(18,5));
        }
        else if(cue==Cue.BodyImpact)
        {
            Oval(-5,12,4,4);
            Curve(P(-7,6),P(-13,0),P(-8,-5));
            Line(P(-4,6),P(0,-4)); Line(P(0,-4),P(-7,-14));
            Line(P(0,-4),P(6,-12)); Line(P(-4,4),P(4,0));
            Line(P(12,14),P(12,-14));
            Line(P(4,7),P(8,5)); Line(P(5,-4),P(8,-5));
        }
        else if(cue==Cue.PoolFloaty)
        {
            Curve(P(-15,-10),P(-20,0),P(-12,11));
            Line(P(-12,11),P(10,11)); Curve(P(10,11),P(19,6),P(15,-10));
            Line(P(15,-10),P(-15,-10)); Line(P(-12,5),P(12,5));
            for(int x=-8;x<=8;x+=8) Line(P(x,1),P(x,-6));
            Line(P(-20,8),P(-23,11)); Line(P(20,9),P(23,12));
        }
        else if(cue==Cue.Cushion)
        {
            Oval(0,-3,15,8); Line(P(2,5),P(5,12)); Line(P(5,12),P(10,12));
        }
        else if(cue==Cue.Jetpack)
        {
            Oval(-7,3,5,12); Oval(7,3,5,12); Line(P(-3,9),P(3,9));
            Line(P(-7,-11),P(-9,-17)); Line(P(7,-11),P(9,-17));
        }
        else if(cue is Cue.SprayPaint or Cue.WeedSpray or Cue.Inhaling)
        {
            Line(P(-8,-9),P(-8,11)); Line(P(-8,11),P(6,11)); Line(P(6,11),P(6,-9)); Line(P(6,-9),P(-8,-9));
            Line(P(0,11),P(0,16)); Line(P(0,16),P(7,16));
            for(int i=0;i<3;i++) Line(P(10,10-i*7),P(19,13-i*9),2);
        }
        else if(cue is Cue.VehicleRattle or Cue.VehicleInteraction or Cue.VehicleEngine or Cue.VehicleDelivery or Cue.VehicleImpact or Cue.VehicleFire or Cue.Skidding)
        {
            Line(P(-16,-7),P(-16,2)); Line(P(-16,2),P(-8,5)); Line(P(-8,5),P(-4,12));
            Line(P(-4,12),P(8,12)); Line(P(8,12),P(13,3)); Line(P(13,3),P(17,1));
            Line(P(17,1),P(17,-7)); Line(P(-16,-7),P(17,-7)); Oval(-9,-9,3,3); Oval(10,-9,3,3);
        }
        else if(cue is Cue.LockPicking or Cue.Unlock)
        {
            Oval(-7,5,6,6); Line(P(-2,1),P(12,-12),4); Line(P(7,-7),P(11,-3)); Line(P(11,-11),P(15,-7));
        }
        else if(cue==Cue.Clock)
        { Oval(0,0,15,15); Line(P(0,10),P(0,0)); Line(P(0,0),P(8,-4)); }
        else if(cue is Cue.EggCall or Cue.EggCry or Cue.EggScream or Cue.EggBreak)
        { Oval(0,0,11,15);
          if(cue==Cue.EggBreak) { Line(P(-10,3),P(-3,-2)); Line(P(-3,-2),P(3,4)); Line(P(3,4),P(10,-1)); }
          else { Line(P(14,5),P(19,9)); Line(P(15,-3),P(21,-3)); if(cue is Cue.EggCry or Cue.EggScream) Line(P(-14,5),P(-19,9)); if(cue==Cue.EggScream) { Line(P(0,8),P(0,1),4); Oval(0,-5,2,2,40,true); } } }
        else if(cue is Cue.SpikeSlam or Cue.SpikeCreak)
        {
            Line(P(-17,12),P(17,12)); Line(P(-17,-13),P(17,-13));
            for(int x=-12;x<=12;x+=12) { Line(P(x-4,11),P(x,-4)); Line(P(x,-4),P(x+4,11)); }
        }
        else if(cue is Cue.Cabinet or Cue.Elevator or Cue.Vent)
        {
            Line(P(-14,-14),P(-14,14)); Line(P(-14,14),P(14,14)); Line(P(14,14),P(14,-14)); Line(P(14,-14),P(-14,-14));
            if(cue==Cue.Vent) { for(int y=-9;y<=9;y+=6) Line(P(-9,y),P(9,y)); }
            else { Line(P(0,-14),P(0,14)); Line(P(-5,-2),P(-5,3)); Line(P(5,-2),P(5,3)); }
        }
        else if(cue is Cue.Terminal or Cue.Phone or Cue.Teeth or Cue.Hairdryer or Cue.Breaker or Cue.Lever)
        {
            if(cue==Cue.Teeth) { Curve(P(-14,9),P(0,15),P(14,9)); Curve(P(-14,-9),P(0,-15),P(14,-9)); for(int x=-10;x<=10;x+=5) { Line(P(x,10),P(x,3)); Line(P(x,-10),P(x,-3)); } }
            else if(cue==Cue.Lever || cue==Cue.Breaker) { Line(P(-13,-12),P(13,-12),4); Line(P(0,-12),P(8,10),4); Oval(9,12,4,4); }
            else if(cue==Cue.Hairdryer) { Oval(0,7,13,7); Line(P(2,0),P(-3,-13),5); Line(P(14,8),P(20,8)); }
            else if(cue==Cue.Phone) { Curve(P(-12,-9),P(-13,10),P(9,12),5); Line(P(-13,-8),P(-4,-12),6); Line(P(7,12),P(12,4),6); }
            else { Box(-14,-8,28,21); Line(P(-9,8),P(-4,4)); Line(P(-4,4),P(-9,0)); Line(P(1,0),P(8,0)); Line(P(-17,-13),P(17,-13),4); }
        }
        else if(cue==Cue.DuckQuack)
        {
            Curve(P(-17,0),P(-11,-20),P(7,-11)); Curve(P(7,-11),P(13,-8),P(10,1));
            Curve(P(10,1),P(17,12),P(6,15)); Curve(P(6,15),P(-3,15),P(0,3));
            Curve(P(0,3),P(-8,-3),P(-17,0)); Line(P(12,7),P(20,5)); Line(P(20,5),P(12,2));
            Curve(P(-10,-4),P(-4,-11),P(2,-6)); Oval(8,10,1.5f,1.5f,40,true);
        }
        else if(cue is Cue.CashRegister or Cue.GiftOpen or Cue.BagZip or Cue.FlashlightSwitch)
        {
            if(cue==Cue.FlashlightSwitch) { Box(-12,-5,17,10); Line(P(5,-5),P(11,-9)); Line(P(11,-9),P(11,9)); Line(P(11,9),P(5,5)); Line(P(16,0),P(21,0)); }
            else if(cue==Cue.BagZip) { Line(P(-4,-15),P(-4,10),2); Line(P(4,-15),P(4,10),2); for(int y=-13;y<=2;y+=5) Line(P(-3,y),P(3,y),2); Box(-6,4,12,8); Oval(0,14,3,5); }
            else { Box(-13,-12,26,20); Line(P(-15,10),P(15,10));
                if(cue==Cue.GiftOpen) { Line(P(0,-12),P(0,10)); Curve(P(0,10),P(-17,23),P(-10,10)); Curve(P(0,10),P(17,23),P(10,10)); }
                else { Box(-8,10,16,7); Line(P(-6,-6),P(6,-6)); }
            }
        }
        else if(cue==Cue.SupplyLanding)
        {
            Line(P(-12,3),P(12,3)); Line(P(12,3),P(12,-13)); Line(P(12,-13),P(-12,-13)); Line(P(-12,-13),P(-12,3));
            Line(P(0,17),P(0,7)); Line(P(-5,12),P(0,7)); Line(P(0,7),P(5,12));
        }
        else if(cue is Cue.MaskLaugh or Cue.MaskSound or Cue.MaskInfection)
        {
            Oval(0,0,11,15); Oval(-5,4,2,2,40,true); Oval(5,4,2,2,40,true);
            Curve(P(-6,-4),P(0,-12),P(6,-4));
        }
        else if(cue==Cue.SkitterFootsteps)
        { Shoe(-10,8,-12,0.8f); Shoe(9,0,15,0.8f); Shoe(-6,-12,-12,0.65f); }
        else if(cue is Cue.OtherCling or Cue.Crawling)
        {
            Oval(0,0,4,12);
            for(int i=0;i<3;i++) { Line(P(-3,6-i*6),P(-12,9-i*6)); Line(P(3,6-i*6),P(12,9-i*6)); }
        }
        else if (cue is Cue.Door or Cue.DoorOpen or Cue.DoorClose)
        {
            Line(P(-13,-14),P(-13,14)); Line(P(-13,14),P(10,14));
            Line(P(10,14),P(10,-14)); Line(P(-16,-14),P(15,-14));
            Line(P(-10,11),P(3,6)); Line(P(3,6),P(3,-13));
            Line(P(-10,11),P(-10,-13)); Line(P(-2,-3),P(-1,-3),2.5f);
        }
        else if (cue is Cue.Alarm or Cue.EquipmentWarning or Cue.BatteryWarning or Cue.PinPull)
        {
            Line(P(0,16),P(-15,-12)); Line(P(-15,-12),P(15,-12)); Line(P(15,-12),P(0,16));
            Line(P(0,7),P(0,-2)); Line(P(0,-7),P(0,-8),3.5f);
        }
        else if (cue == Cue.Snip)
        {
            Oval(-7,-8,4,4,12); Oval(7,-8,4,4,12);
            Line(P(-5,-5),P(11,13)); Line(P(5,-5),P(-11,13));
        }
        else if(cue==Cue.Breathing)
        { Curve(P(-18,8),P(-4,17),P(4,8)); Curve(P(-18,0),P(1,9),P(15,0)); Curve(P(-13,-9),P(6,-1),P(20,-9)); }
        else if(cue is Cue.Drum or Cue.March)
        { Oval(0,4,13,5); Line(P(-13,4),P(-13,-11)); Line(P(13,4),P(13,-11)); Curve(P(-13,-11),P(0,-19),P(13,-11));
          Line(P(-16,19),P(-3,9)); Line(P(16,19),P(3,9)); if(cue==Cue.March) { Line(P(-17,-20),P(-7,-20)); Line(P(7,-20),P(17,-20)); } }
        else if(cue==Cue.Lunge)
        { Line(P(-18,0),P(17,0),4); Line(P(5,12),P(17,0),4); Line(P(17,0),P(5,-12),4); Line(P(-16,9),P(-4,9)); Line(P(-16,-9),P(-4,-9)); }
        else if(cue is Cue.Clicking or Cue.Chitter or Cue.Rattle)
        { for(int j=0;j<3;j++) { float y=(j%2)*8-4; Line(P(-15+j*12,y+5),P(-10+j*12,y)); Line(P(-10+j*12,y),P(-15+j*12,y-5)); }
          if(cue==Cue.Rattle) { Line(P(-13,-15),P(13,-15)); } }
        else if(cue==Cue.Engine)
        { Box(-12,-10,24,18); Line(P(-6,13),P(6,13)); Line(P(-17,-5),P(-17,3)); Line(P(12,2),P(19,2)); Line(P(19,2),P(19,-7)); }
        else if(cue==Cue.Electric)
        { Line(P(4,19),P(-10,-2),4); Line(P(-10,-2),P(4,-2),4); Line(P(4,-2),P(-4,-19),4); Line(P(-18,9),P(-13,5)); Line(P(13,-5),P(18,-9)); }
        else if(cue==Cue.Spit)
        { Curve(P(-17,-7),P(-2,16),P(17,5)); Oval(14,-4,3,4); Oval(7,-12,2,3); Line(P(-17,-7),P(-9,-7)); }
        else if(cue==Cue.Sliding)
        { Line(P(-17,-12),P(17,-12)); Curve(P(-13,2),P(0,-6),P(13,2)); Line(P(6,9),P(16,9)); Line(P(11,14),P(16,9)); Line(P(16,9),P(11,4)); }
        else if(cue==Cue.Flopping)
        { Curve(P(-13,-4),P(0,18),P(13,-4)); Curve(P(-13,-4),P(0,-14),P(13,-4)); Line(P(-17,-12),P(-21,-5)); Line(P(17,-12),P(21,-5)); Line(P(-7,-18),P(7,-18)); }
        else if(cue==Cue.Whining)
        { Oval(-6,0,7,10); Curve(P(5,10),P(21,0),P(5,-10)); Line(P(13,-16),P(20,-16)); }
        else if (family == SoundFamily.Music)
        {
            Line(P(-6,-8),P(-6,12)); Line(P(-6,12),P(10,16)); Line(P(10,16),P(10,-4));
            Line(P(-13,-9),P(-5,-9),6); Line(P(3,-5),P(11,-5),6);
        }
        else if (family == SoundFamily.Mechanical)
        {
            Oval(0,0,10,10); Oval(0,0,3,3);
            for(int j=0;j<8;j++) { float a=j*Mathf.PI/4; Line(P(Mathf.Cos(a)*11,Mathf.Sin(a)*11),P(Mathf.Cos(a)*17,Mathf.Sin(a)*17),4); }
        }
        else if (family == SoundFamily.Ground)
        {
            Line(P(-15,-4),P(-7,4)); Line(P(-7,4),P(0,-7)); Line(P(0,-7),P(8,5)); Line(P(8,5),P(15,-4));
            Line(P(-14,-13),P(14,-13));
        }
        else if (family == SoundFamily.Movement)
        {
            // Staggered shoe soles with a flat split and rounded heel, not ovals.
            float soleSize=cue==Cue.SmallFootsteps ? 0.65f : 1;
            Shoe(-8,-2,10,soleSize); Shoe(8,6,-10,soleSize);
            if(cue==Cue.DragFootsteps) { Line(P(-17,-15),P(-4,-15),2); Line(P(4,-9),P(17,-9),2); }
            if(cue==Cue.HeavyFootsteps) { Line(P(-15,-16),P(-3,-16),4); Line(P(3,-10),P(15,-10),4); }
            if(cue==Cue.Running) { Line(P(-18,5),P(-13,5),2); Line(P(13,-7),P(19,-7),2); }
            if(cue==Cue.BootFootsteps) { Line(P(-15,-16),P(-3,-16),4); Line(P(3,-10),P(15,-10),4); Line(P(-16,3),P(-16,10)); }
            if(cue==Cue.BareFootsteps) { for(int j=0;j<3;j++) { Oval(-12+j*3,12,1,1,40,true); Oval(4+j*3,19,1,1,40,true); } }
            if(cue==Cue.SkippingFootsteps) Curve(P(-17,7),P(0,23),P(17,12),2);
        }
        else if (family == SoundFamily.Liquid)
        {
            Line(P(0,15),P(-9,-1)); Line(P(0,15),P(9,-1));
            for(int j=0;j<12;j++)
            {
                float a=Mathf.PI+j*Mathf.PI/12,b=Mathf.PI+(j+1)*Mathf.PI/12;
                Line(P(Mathf.Cos(a)*9,-1+Mathf.Sin(a)*10),P(Mathf.Cos(b)*9,-1+Mathf.Sin(b)*10));
            }
        }
        else if (family == SoundFamily.Insects)
        {
            Oval(-9,5,6,5); Oval(9,5,6,5); Oval(0,-4,2,7,40,true);
            Line(P(-4,3),P(-12,7),1.5f); Line(P(4,3),P(12,7),1.5f);
            Line(P(-2,11),P(-5,15)); Line(P(2,11),P(5,15));
        }
        else if (family == SoundFamily.Air)
        {
            Curve(P(0,-3),P(-6,15),P(-19,10));
            Curve(P(-19,10),P(-14,-5),P(-3,-6));
            Curve(P(0,-3),P(6,15),P(19,10));
            Curve(P(19,10),P(14,-5),P(3,-6));
            Curve(P(-14,6),P(-10,0),P(-5,-2),1.5f);
            Curve(P(14,6),P(10,0),P(5,-2),1.5f);
        }
        else if (family == SoundFamily.Personal)
        {
            Line(P(-15,0),P(-7,0)); Line(P(-7,0),P(-3,10));
            Line(P(-3,10),P(3,-10)); Line(P(3,-10),P(7,0)); Line(P(7,0),P(15,0));
        }
        else if (family == SoundFamily.Impact)
        {
            for(int j=0;j<6;j++)
            {
                float a=j*Mathf.PI/3;
                Line(P(Mathf.Cos(a)*5,Mathf.Sin(a)*5),P(Mathf.Cos(a)*16,Mathf.Sin(a)*16));
            }
        }
        else
        {
            Line(P(-13,-9),P(-9,10)); Line(P(-2,-12),P(1,13)); Line(P(9,-9),P(13,10));
        }
        glyphScale=1; offset=0;
        // Center the visible group, including its stroke length, at every intensity.
        for (int j = 0; j < strength; j++)
        {
            float center=(j-(strength-1)*0.5f)*7;
            Line(P(center-1.5f,-22),P(center+1.5f,-22),2.5f);
        }
    }
}
