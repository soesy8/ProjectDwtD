using UnityEngine;
using UnityEngine.UI;

namespace DwtD.TestScene
{
    // Paired open palms, drawn as native UI geometry at any canvas resolution.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CorridorHandsGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            void Disc(Vector2 center,Vector2 radius,Color tint)
            {
                int start=vh.currentVertCount;
                vh.AddVert(center,tint,Vector2.zero);
                for(int i=0;i<=24;i++)
                {
                    float a=i*Mathf.PI*2/24;
                    vh.AddVert(center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y),tint,Vector2.zero);
                    if(i>0) vh.AddTriangle(start,start+i,start+i+1);
                }
            }
            var r=rectTransform.rect;float scale=Mathf.Min(r.width/2.2f,r.height/1.45f);
            for(int side=-1;side<=1;side+=2)
            {
                Vector2 Map(Vector2 p)=>r.center+new Vector2(side*(.56f+p.x),p.y)*scale;
                void Capsule(Vector2 a,Vector2 b,float width,Color tint)
                {
                    a=Map(a);b=Map(b);float radius=width*scale*.5f;
                    Disc(a,Vector2.one*radius,tint);Disc(b,Vector2.one*radius,tint);
                    var n=new Vector2(-(b-a).y,(b-a).x).normalized*radius;int i=vh.currentVertCount;
                    vh.AddVert(a+n,tint,Vector2.zero);vh.AddVert(a-n,tint,Vector2.zero);
                    vh.AddVert(b-n,tint,Vector2.zero);vh.AddVert(b+n,tint,Vector2.zero);
                    vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
                }
                foreach(bool border in new[]{true,false})
                {
                    Color tint=border?CorridorUITheme.Ink:new Color(1,.77f,.55f);float extra=border?.055f:0;
                    Disc(Map(new Vector2(0,-.03f)),new Vector2(.25f+extra*.5f,.31f+extra*.5f)*scale,tint);
                    Capsule(new Vector2(0,-.26f),new Vector2(0,-.53f),.23f+extra,tint);
                    for(int i=0;i<4;i++)
                    {
                        float x=-.19f+i*.125f;
                        Capsule(new Vector2(x,.1f),new Vector2(x-.04f,.40f+Mathf.Sin((i+1)*Mathf.PI/5)*.13f),.115f+extra,tint);
                    }
                    Capsule(new Vector2(.18f,-.10f),new Vector2(.37f,.12f),.15f+extra,tint);
                }
            }
        }
    }
}
