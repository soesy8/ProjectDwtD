using TMPro;
using UnityEngine;

namespace DwtD.TestScene
{
    // Shared, idempotent styling for the existing scene and the runtime story controls.
    public static class CorridorUITheme
    {
        public static readonly Color Ink=new Color(.28f,.19f,.14f);
        public static readonly Color Paper=new Color(1f,.965f,.86f);
        public static readonly Color Teal=new Color(.16f,.48f,.44f);
        public static readonly Color Mint=new Color(.79f,.89f,.76f);
        static readonly Color MutedTeal=new Color(.43f,.59f,.51f);

        static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var rect=parent.Find(name) as RectTransform;
            if(rect==null) rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        static CorridorBubbleGraphic Shape(RectTransform rect,Color fill,float radius,float depth=10,float border=3)
        {
            var shape=rect.GetComponent<CorridorBubbleGraphic>();
            if(shape==null) shape=rect.gameObject.AddComponent<CorridorBubbleGraphic>();
            shape.color=fill;shape.borderColor=Ink;shape.cornerRadius=radius;shape.borderWidth=border;
            shape.extrusion=depth;shape.bevelWidth=Mathf.Min(8,rect.rect.height*.08f);
            shape.shadowOffset=new Vector2(5,-depth-4);shape.shadowOpacity=.15f;
            shape.raycastTarget=false;shape.SetAllDirty();return shape;
        }
        static CorridorBubbleGraphic Surface(Transform host,Color fill,float radius,float depth=12)
        {
            var oldImage=host.GetComponent<UnityEngine.UI.Image>();if(oldImage!=null) { oldImage.enabled=false;oldImage.raycastTarget=false; }
            var rect=Rect("CelSurface",host,Vector2.zero,Vector2.zero);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;rect.SetAsFirstSibling();
            return Shape(rect,fill,radius,depth,4);
        }
        static CorridorBubbleGraphic Badge(string name,Transform parent,Vector2 size,Vector2 position,Color fill,float radius=24,float depth=5)
        {
            var rect=Rect(name,parent,size,position);rect.SetSiblingIndex(Mathf.Min(1,parent.childCount-1));
            var shape=Shape(rect,fill,radius,depth,2.5f);shape.shadowOpacity=0;return shape;
        }
        static void TextStyle(TMP_Text text,Color color,float size,bool bold=false)
        {
            if(text==null) return;
            text.color=color;text.fontSize=size;if(bold) text.fontStyle=FontStyles.Bold;
        }
        static void Card(Transform panel,string name)
        {
            var card=panel.Find(name);if(card!=null) Surface(card,Paper,36,14);
        }

        public static void StyleGuide(Transform panel,float depth)=>Surface(panel,Paper,28,depth);

        public static void Apply(WetCorridorPrototype owner)
        {
            StyleSpeech(owner);
            var header=owner.heading.GetComponentInParent<Canvas>(true).transform;
            header.position=new Vector3(0,3.45f,-1.1f);header.localScale=Vector3.one*.0019f;
            Card(header,"HeaderCard");
            var accent=header.Find("Accent")?.GetComponent<UnityEngine.UI.Image>();if(accent!=null) accent.enabled=false;
            Badge("ChapterBadge",header,new Vector2(340,44),new Vector2(0,70),Teal,18,5);
            var stage=header.Find("StageLabel")?.GetComponent<TMP_Text>();TextStyle(stage,Paper,24,true);
            TextStyle(owner.heading,Ink,48,true);TextStyle(owner.subtitle,new Color(.43f,.34f,.26f),29);

            var book=owner.stageCounter.GetComponentInParent<Canvas>(true).transform;
            book.position=new Vector3(-2.8f,2.1f,-.55f);book.localScale=Vector3.one*.0019f;
            Card(book,"NotebookCover");
            var spine=book.Find("NotebookSpine");if(spine!=null) { var s=Surface(spine,Teal,7,5);s.shadowOpacity=0;s.borderWidth=2; }
            Badge("NotebookTitleTab",book,new Vector2(240,48),new Vector2(8,150),Teal,16,5);
            TextStyle(book.Find("NotebookTitle")?.GetComponent<TMP_Text>(),Paper,28,true);
            var well=Badge("StampWell",book,new Vector2(132,132),new Vector2(0,47),Mint,66,5);well.borderColor=MutedTeal;
            var empty=book.Find("EmptyStamp")?.GetComponent<CorridorSymbolGraphic>();if(empty!=null) empty.color=MutedTeal;
            var earned=owner.stampMark.GetComponent<CorridorSymbolGraphic>();if(earned!=null) earned.color=Teal;
            var caption=book.Find("StampCaption")?.GetComponent<TMP_Text>();TextStyle(caption,Ink,25,true);
            if(caption!=null) caption.rectTransform.anchoredPosition=new Vector2(0,-43);
            foreach(var icon in book.GetComponentsInChildren<CorridorSymbolGraphic>(true))
                if(icon.name=="NextStamp") icon.color=MutedTeal;
            Badge("FutureStampLeft",book,new Vector2(54,54),new Vector2(-67,-94),Paper,27,4);
            Badge("FutureStampRight",book,new Vector2(54,54),new Vector2(67,-94),Paper,27,4);
            Badge("NotebookCount",book,new Vector2(128,45),new Vector2(0,-153),Mint,18,4);
            TextStyle(owner.stageCounter,Ink,28,true);
            for(int i=0;i<4;i++)
            {
                var ring=Badge("BindingRing_"+i,book,new Vector2(32,17),new Vector2(-151,125-i*83),Paper,8,3);
                ring.borderWidth=2;ring.borderColor=Teal;
            }

            Card(owner.rewardPanel.transform,"PraiseCard");
            Badge("RewardStampMedallion",owner.rewardPanel.transform,new Vector2(205,205),new Vector2(-380,44),Mint,102,9);
            var rewardIcon=owner.rewardPanel.transform.Find("EyeStamp")?.GetComponent<CorridorSymbolGraphic>();if(rewardIcon!=null) rewardIcon.color=Teal;
            owner.rewardText.rectTransform.anchoredPosition=new Vector2(130,70);
            owner.rewardText.rectTransform.sizeDelta=new Vector2(760,170);
            TextStyle(owner.rewardText,Ink,34,true);
            if(owner.finalFeedback!=null)
            {
                Card(owner.finalFeedback.questionPanel.transform,"QuestionCard");Card(owner.finalFeedback.completedPanel.transform,"EpilogueCard");
                TextStyle(owner.finalFeedback.questionTitle,Ink,38,true);TextStyle(owner.finalFeedback.questionFeedback,Ink,30);
            }
            if(owner.thoughtBubble!=null)
            {
                var thoughts=owner.thoughtBubble;
                thoughts.viewportAnchor=new Vector2(.20f,.74f);thoughts.viewportHeight=.28f;
                thoughts.bubbleRoot.sizeDelta=new Vector2(600,380);
                var cloud=thoughts.bubbleRoot.GetComponent<CorridorBubbleGraphic>();
                cloud.color=Paper;cloud.borderColor=Ink;cloud.borderWidth=4;cloud.extrusion=14;cloud.bevelWidth=10;cloud.SetAllDirty();
                Badge("ThoughtTitleBadge",thoughts.bubbleRoot,new Vector2(200,58),new Vector2(-140,206),Paper,26,5);
                thoughts.thoughtLabel.rectTransform.anchoredPosition=new Vector2(-140,206);
                thoughts.thoughtLabel.rectTransform.sizeDelta=new Vector2(190,56);
                thoughts.thought.rectTransform.anchoredPosition=new Vector2(0,5);
                thoughts.thought.rectTransform.sizeDelta=new Vector2(530,300);
                // The cloud is the canvas root, so all text must remain above its title chip.
                thoughts.thoughtLabel.transform.SetAsLastSibling();thoughts.thought.transform.SetAsLastSibling();
                TextStyle(thoughts.thoughtLabel,Ink,34,true);TextStyle(thoughts.thought,Ink,62,true);
            }
            LayoutComposition(owner);
        }

        static void LayoutComposition(WetCorridorPrototype owner)
        {
            var start=(RectTransform)owner.startButton.transform;
            var title=(RectTransform)owner.titlePanel.transform;
            foreach(Transform child in title) if(child!=start) child.gameObject.SetActive(false);
            title.sizeDelta=new Vector2(250,250);start.sizeDelta=new Vector2(250,250);start.anchoredPosition=Vector2.zero;
            CorridorComposition.PlacePanel(title,owner.playerCamera,new Vector2(.86f,.16f),.19f);
            foreach(var panel in new[]{owner.choicePanel,owner.pathPanel,owner.retryPanel})
                CorridorComposition.PlacePanel((RectTransform)panel.transform,owner.playerCamera,new Vector2(.58f,.17f),.15f);
            CorridorComposition.PlacePanel((RectTransform)owner.rewardPanel.transform,owner.playerCamera,new Vector2(.56f,.20f),.28f);
            var book=(RectTransform)owner.stageCounter.GetComponentInParent<Canvas>(true).transform;
            CorridorComposition.PlacePanel(book,owner.playerCamera,new Vector2(.14f,.23f),.29f);
            owner.heading.GetComponentInParent<Canvas>(true).gameObject.SetActive(false);
            book.gameObject.SetActive(false);
        }

        static void StyleSpeech(WetCorridorPrototype owner)
        {
            var speech=owner.bubble.transform.parent as RectTransform;
            if(speech==null) return;
            speech.sizeDelta=new Vector2(530,125);owner.bubble.rectTransform.sizeDelta=new Vector2(500,110);
            foreach(var image in speech.GetComponentsInChildren<UnityEngine.UI.Image>(true)) image.enabled=false;
            var rect=Rect("ComicSpeechSurface",speech,new Vector2(530,125),Vector2.zero);rect.SetAsFirstSibling();
            var shape=Shape(rect,Paper,42,10,4);shape.speechTail=true;shape.bevelWidth=8;
            TextStyle(owner.bubble,Ink,44,true);
        }

        public static void StyleButton(UnityEngine.UI.Button button)
        {
            if(button.name=="Start" || button.name=="OpenSignalChoices")
            {
                StyleActionButton(button);return;
            }
            bool primary=button.name=="Start" || button.name=="CollectStamp" || button.name=="OpenSignalChoices" || button.name=="PointToPath" || button.name=="Next";
            var fill=primary?Teal:Paper;
            var graphic=Surface(button.transform,fill,30,13);graphic.raycastTarget=true;
            button.targetGraphic=graphic;button.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=colors.pressedColor=Color.white;
            colors.disabledColor=new Color(.72f,.72f,.66f,.85f);colors.fadeDuration=.08f;button.colors=colors;
            var label=button.GetComponentInChildren<TMP_Text>(true);
            var symbol=button.GetComponentInChildren<CorridorSymbolGraphic>(true);
            if(symbol==null && (button.name=="OpenSignalChoices" || button.name=="Hint"))
            {
                var rect=Rect("Symbol",button.transform,new Vector2(62,62),Vector2.zero);
                symbol=rect.gameObject.AddComponent<CorridorSymbolGraphic>();symbol.raycastTarget=false;
                symbol.symbol=button.name=="Hint"?CorridorSymbolGraphic.Symbol.Question:CorridorSymbolGraphic.Symbol.Wave;
            }
            var size=((RectTransform)button.transform).rect.size;
            if(symbol!=null)
            {
                bool wide=size.x>size.y*2.5f;
                float diameter=wide?Mathf.Min(86,size.y*.66f):116;
                var position=wide?new Vector2(-size.x*.5f+diameter*.75f+18,0):new Vector2(0,28);
                var symbolRect=(RectTransform)symbol.transform;symbolRect.sizeDelta=Vector2.one*diameter*.66f;symbolRect.anchoredPosition=position;
                var badge=Badge("IconMedallion",button.transform,Vector2.one*diameter,position,primary?new Color(.26f,.59f,.52f):Mint,diameter*.5f,5);
                badge.borderColor=primary?Paper:Teal;badge.borderWidth=2.2f;
                symbol.color=primary?Paper:Teal;symbol.transform.SetAsLastSibling();
                if(label!=null && wide)
                {
                    label.rectTransform.sizeDelta=new Vector2(size.x-diameter-70,size.y-24);
                    label.rectTransform.anchoredPosition=new Vector2(diameter*.4f+8,0);
                }
            }
            if(label!=null) { label.color=primary?Paper:Ink;label.fontStyle=FontStyles.Bold;label.fontSize=button.name=="Next"?28:32;label.transform.SetAsLastSibling(); }
            if(button.name=="Hint" && label!=null)
            {
                label.text="힌트";label.fontSize=30;label.rectTransform.sizeDelta=new Vector2(140,62);label.rectTransform.anchoredPosition=new Vector2(30,0);
            }
            var depth=button.GetComponent<CorridorButtonDepth>();if(depth==null) depth=button.gameObject.AddComponent<CorridorButtonDepth>();depth.Initialize(graphic);
        }

        static void StyleActionButton(UnityEngine.UI.Button button)
        {
            var size=((RectTransform)button.transform).rect.size;
            var face=Surface(button.transform,Paper,40,12);face.raycastTarget=true;
            button.targetGraphic=face;button.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.pressedColor=colors.selectedColor=Color.white;
            button.colors=colors;
            foreach(var icon in button.GetComponentsInChildren<CorridorSymbolGraphic>(true)) icon.gameObject.SetActive(false);
            var oldBadge=button.transform.Find("IconMedallion");if(oldBadge!=null) oldBadge.gameObject.SetActive(false);
            var rect=Rect("PairedHands",button.transform,new Vector2(size.x*.78f,size.y*.58f),new Vector2(0,size.y*.12f));
            if(rect.GetComponent<CanvasRenderer>()==null) rect.gameObject.AddComponent<CanvasRenderer>();
            var hands=rect.GetComponent<CorridorHandsGraphic>();if(hands==null) hands=rect.gameObject.AddComponent<CorridorHandsGraphic>();hands.raycastTarget=false;
            var label=button.GetComponentInChildren<TMP_Text>(true);
            if(label!=null)
            {
                label.text=button.name=="Start"?"이야기 시작":"행동 선택";label.color=Ink;label.fontSize=38;label.fontStyle=FontStyles.Bold;
                label.rectTransform.sizeDelta=new Vector2(size.x-20,size.y*.25f);
                label.rectTransform.anchoredPosition=new Vector2(0,-size.y*.31f);label.transform.SetAsLastSibling();
            }
            var depth=button.GetComponent<CorridorButtonDepth>();if(depth==null) depth=button.gameObject.AddComponent<CorridorButtonDepth>();depth.Initialize(face);
        }
    }
}
