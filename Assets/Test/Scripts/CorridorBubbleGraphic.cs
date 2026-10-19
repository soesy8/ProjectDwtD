using System.Collections.Generic;
using UnityEngine;

namespace DwtD.TestScene
{
    // Shading stays in the UI plane: the same crisp color bands are visible to both eyes.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CorridorBubbleGraphic : UnityEngine.UI.MaskableGraphic
    {
        public bool thoughtCloud, speechTail;
        public Color borderColor=new Color(.28f,.19f,.14f,1);
        public float borderWidth=4, cornerRadius=28;
        [Header("Cel shaded volume")]
        [Min(0)] public float extrusion=11, bevelWidth=7;
        [Range(0,1)] public float shadeStrength=.25f;
        public Vector2 shadowOffset=new Vector2(6,-12);
        [Range(0,.5f)] public float shadowOpacity=.16f;
        bool hovered,pressed;

        public void SetInteraction(bool hover,bool press)
        {
            if(hovered==hover && pressed==press) return;
            hovered=hover; pressed=press; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear(); var r=rectTransform.rect;
            if(r.width<=0 || r.height<=0) return;
            float depth=pressed?extrusion*.28f:extrusion;
            Color fill=hovered?Color.Lerp(color,Color.white,.12f):color;
            if(speechTail) Tail(vh,r,fill,depth);
            Volume(vh,Contour(r,cornerRadius,thoughtCloud),r.center,fill,borderWidth,depth);
            if(thoughtCloud)
            {
                Dot(vh,r.center+new Vector2(-r.width*.28f,-r.height*.54f),15,fill);
                Dot(vh,r.center+new Vector2(-r.width*.33f,-r.height*.68f),9,fill);
            }
        }
        void Dot(UnityEngine.UI.VertexHelper vh,Vector2 center,float radius,Color fill)
        {
            Volume(vh,Contour(new Rect(center-Vector2.one*radius,Vector2.one*radius*2),radius,false),center,fill,Mathf.Min(borderWidth,2.5f),extrusion*.35f);
        }
        void Volume(UnityEngine.UI.VertexHelper vh,List<Vector2> outer,Vector2 center,Color fill,float border,float depth)
        {
            if(shadowOpacity>0) Polygon(vh,Offset(outer,shadowOffset),new Color(.20f,.12f,.08f,shadowOpacity*fill.a));
            var side=Color.Lerp(fill,new Color(.42f,.29f,.20f,fill.a),shadeStrength+.12f);
            Polygon(vh,Offset(outer,Vector2.down*depth),borderColor);
            var face=Expand(outer,center,-border);
            Polygon(vh,Offset(face,Vector2.down*depth),side);
            if(hovered && !pressed) Polygon(vh,Expand(outer,center,2.5f),new Color(.20f,.55f,.48f,fill.a));
            Polygon(vh,outer,borderColor); Polygon(vh,face,fill);
            float low=float.MaxValue,high=float.MinValue;
            foreach(var p in face) { low=Mathf.Min(low,p.y);high=Mathf.Max(high,p.y); }
            Polygon(vh,ClipY(face,Mathf.Lerp(low,high,.28f),false),Color.Lerp(fill,side,.40f));
            Polygon(vh,ClipY(face,Mathf.Lerp(low,high,.80f),true),Color.Lerp(fill,Color.white,.32f));
            var inner=Expand(face,center,-Mathf.Min(bevelWidth,(high-low)*.12f));
            for(int i=0;i<face.Count;i++)
            {
                int next=(i+1)%face.Count;
                var normal=((face[i]+face[next])*.5f-center).normalized;
                float light=Vector2.Dot(normal,new Vector2(-.48f,.88f));
                Color rim=light>.20f?Color.Lerp(fill,Color.white,.70f):light<-.20f?Color.Lerp(fill,side,.78f):Color.Lerp(fill,side,.20f);
                Quad(vh,face[i],face[next],inner[next],inner[i],rim);
            }
        }
        void Tail(UnityEngine.UI.VertexHelper vh,Rect r,Color fill,float depth)
        {
            float x=r.center.x+r.width*.12f,y=r.yMin+borderWidth;
            float half=Mathf.Min(28,r.width*.07f),length=Mathf.Min(43,r.height*.30f);
            var points=new List<Vector2>{new Vector2(x-half,y+12),new Vector2(x+half,y+12),new Vector2(x-half*.45f,y-length)};
            Polygon(vh,Offset(points,shadowOffset),new Color(.20f,.12f,.08f,shadowOpacity));
            Polygon(vh,Offset(points,Vector2.down*depth),borderColor); Polygon(vh,points,borderColor);
            Polygon(vh,new List<Vector2>{new Vector2(x-half+borderWidth,y+14),new Vector2(x+half-borderWidth,y+14),new Vector2(x-half*.45f,y-length+borderWidth*2)},Color.Lerp(fill,new Color(.52f,.37f,.23f),.15f));
        }
        static List<Vector2> Contour(Rect r,float radius,bool cloud)
        {
            var points=new List<Vector2>();
            if(cloud)
            {
                const int count=144;
                for(int i=0;i<count;i++)
                {
                    float a=i*Mathf.PI*2/count,scallop=1+.065f*Mathf.Cos(a*9+.35f);
                    points.Add(r.center+new Vector2(Mathf.Cos(a)*r.width*.475f,Mathf.Sin(a)*r.height*.43f)*scallop);
                }
            }
            else
            {
                float rad=Mathf.Clamp(radius,0,Mathf.Min(r.width,r.height)*.5f);
                for(int corner=0;corner<4;corner++)
                {
                    var c=new Vector2(corner==0 || corner==3?r.xMax-rad:r.xMin+rad,corner<2?r.yMax-rad:r.yMin+rad);
                    for(int j=0;j<=10;j++) { float a=(corner*90+j*9)*Mathf.Deg2Rad;points.Add(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rad); }
                }
            }
            return points;
        }
        static List<Vector2> Offset(List<Vector2> points,Vector2 offset)
        {
            var result=new List<Vector2>(points.Count);foreach(var p in points) result.Add(p+offset);return result;
        }
        static List<Vector2> Expand(List<Vector2> points,Vector2 center,float amount)
        {
            float x=0,y=0;foreach(var p in points) { x=Mathf.Max(x,Mathf.Abs(p.x-center.x));y=Mathf.Max(y,Mathf.Abs(p.y-center.y)); }
            var scale=new Vector2(Mathf.Max(.1f,(x+amount)/Mathf.Max(x,.1f)),Mathf.Max(.1f,(y+amount)/Mathf.Max(y,.1f)));
            var result=new List<Vector2>(points.Count);foreach(var p in points) result.Add(center+Vector2.Scale(p-center,scale));return result;
        }
        static List<Vector2> ClipY(List<Vector2> source,float y,bool above)
        {
            var result=new List<Vector2>();if(source.Count<3) return result;
            Vector2 previous=source[source.Count-1];bool previousInside=above?previous.y>=y:previous.y<=y;
            foreach(var current in source)
            {
                bool inside=above?current.y>=y:current.y<=y;
                if(inside!=previousInside) result.Add(Vector2.LerpUnclamped(previous,current,(y-previous.y)/(current.y-previous.y)));
                if(inside) result.Add(current);
                previous=current;previousInside=inside;
            }
            return result;
        }
        static void Polygon(UnityEngine.UI.VertexHelper vh,List<Vector2> points,Color tint)
        {
            if(points.Count<3) return;
            Vector2 center=Vector2.zero;foreach(var p in points) center+=p;center/=points.Count;
            int first=vh.currentVertCount;vh.AddVert(center,tint,Vector2.zero);
            foreach(var p in points) vh.AddVert(p,tint,Vector2.zero);
            for(int i=0;i<points.Count;i++) vh.AddTriangle(first,first+i+1,first+(i+1)%points.Count+1);
        }
        static void Quad(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
        {
            int first=vh.currentVertCount;
            vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddVert(d,tint,Vector2.zero);
            vh.AddTriangle(first,first+1,first+2);vh.AddTriangle(first,first+2,first+3);
        }
    }
}
