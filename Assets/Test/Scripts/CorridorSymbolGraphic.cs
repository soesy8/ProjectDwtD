using UnityEngine;
using UnityEngine.UI;

namespace DwtD.TestScene
{
    // Vector pictograms keep the prototype readable without emoji/font support.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CorridorSymbolGraphic : MaskableGraphic
    {
        public enum Symbol { Hand, Hurry, Wave, Eye, Check, Drop, Retry, Play, Question, Footprints }
        public Symbol symbol;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float size = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);
            Vector2 origin = rectTransform.rect.center;
            void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                int first = vh.currentVertCount;
                foreach (Vector2 p in new[] { a, b, c, d })
                    vh.AddVert(origin + p * size, color, Vector2.zero);
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first, first + 2, first + 3);
            }
            void Rect(float x, float y, float w, float h) => Quad(
                new Vector2(x-w/2,y-h/2),new Vector2(x-w/2,y+h/2),
                new Vector2(x+w/2,y+h/2),new Vector2(x+w/2,y-h/2));
            void Line(Vector2 a, Vector2 b, float thickness)
            {
                Vector2 n = new Vector2(-(b-a).y,(b-a).x).normalized * thickness/2;
                Quad(a-n,a+n,b+n,b-n);
            }
            void Circle(Vector2 c, float radius, int count = 32)
            {
                int first = vh.currentVertCount;
                vh.AddVert(origin+c*size,color,Vector2.zero);
                for (int i = 0; i <= count; i++)
                {
                    float a = i*Mathf.PI*2/count;
                    vh.AddVert(origin+(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius)*size,color,Vector2.zero);
                    if (i>0) vh.AddTriangle(first,first+i,first+i+1);
                }
            }
            void Arrow(float x)
            {
                Line(new Vector2(x-.13f,-.28f),new Vector2(x+.12f,0),.09f);
                Line(new Vector2(x+.12f,0),new Vector2(x-.13f,.28f),.09f);
            }
            switch (symbol)
            {
                case Symbol.Hand:
                case Symbol.Wave:
                    Rect(.01f,-.12f,.36f,.37f);
                    Rect(.01f,-.34f,.21f,.13f);
                    for(int i=0;i<4;i++)
                    {
                        float x=-.145f+i*.10f, h=.35f-Mathf.Abs(i-1.5f)*.06f;
                        Rect(x,.11f,.075f,h); Circle(new Vector2(x,.11f+h/2),.0375f,12);
                    }
                    Line(new Vector2(-.14f,-.14f),new Vector2(-.29f,.01f),.11f);
                    if(symbol==Symbol.Wave)
                    {
                        Line(new Vector2(.29f,.16f),new Vector2(.40f,.25f),.045f);
                        Line(new Vector2(.27f,.31f),new Vector2(.33f,.43f),.045f);
                    }
                    break;
                case Symbol.Hurry: Arrow(-.16f); Arrow(.16f); break;
                case Symbol.Check:
                    Line(new Vector2(-.32f,-.01f),new Vector2(-.08f,-.25f),.115f);
                    Line(new Vector2(-.08f,-.25f),new Vector2(.35f,.29f),.115f); break;
                case Symbol.Eye:
                    for(int i=0;i<20;i++)
                    {
                        float x=-.43f+i*.043f, nx=x+.043f;
                        float y=.24f*Mathf.Sin((x+.43f)/.86f*Mathf.PI);
                        float ny=.24f*Mathf.Sin((nx+.43f)/.86f*Mathf.PI);
                        Line(new Vector2(x,y),new Vector2(nx,ny),.055f);
                        Line(new Vector2(x,-y),new Vector2(nx,-ny),.055f);
                    }
                    Circle(Vector2.zero,.12f); break;
                case Symbol.Drop:
                    Circle(new Vector2(0,-.14f),.25f);
                    Quad(new Vector2(-.23f,-.05f),new Vector2(0,.43f),new Vector2(.23f,-.05f),new Vector2(0,-.25f)); break;
                case Symbol.Play:
                    Quad(new Vector2(-.24f,-.35f),new Vector2(-.24f,.35f),new Vector2(.35f,0),new Vector2(.35f,0)); break;
                case Symbol.Retry:
                    for(int i=0;i<26;i++)
                    {
                        float a=(35+i*10)*Mathf.Deg2Rad,b=a+10*Mathf.Deg2Rad;
                        Line(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.30f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.30f,.07f);
                    }
                    Quad(new Vector2(.30f,.33f),new Vector2(.04f,.28f),new Vector2(.28f,.08f),new Vector2(.28f,.08f)); break;
                case Symbol.Footprints:
                    Circle(new Vector2(-.13f,.10f),.105f); Rect(-.13f,-.08f,.15f,.23f);
                    Circle(new Vector2(.15f,.27f),.105f); Rect(.15f,.09f,.15f,.23f); break;
                default:
                    Rect(0,-.02f,.065f,.18f); Circle(new Vector2(0,-.25f),.055f);
                    Line(new Vector2(-.15f,.24f),new Vector2(0,.34f),.07f);
                    Line(new Vector2(0,.34f),new Vector2(.15f,.20f),.07f);
                    Line(new Vector2(.15f,.20f),new Vector2(0,.07f),.07f); break;
            }
        }
    }
}
