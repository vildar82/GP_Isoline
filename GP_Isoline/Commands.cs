using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GP_Isoline.Model;

namespace GP_Isoline
{
   public class Commands
   {
      private static IsolineDrawableOverrule _overruleIsolineDraw = null;
      private static IsolineTransformOverrule _overruleIsolineTrans = null;

      [CommandMethod("PIK", "GP-Isoline", CommandFlags.Modal)]
      public void GpIsolines()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if (doc == null) return;

         Isoline.RegAppIsoline();
         Editor ed = doc.Editor;

         var optKeywords = new PromptKeywordOptions(
            $"Отрисовка бергштрихов для полилиний {(_overruleIsolineDraw == null ? "Отключена" : "Включена")}");

         optKeywords.Keywords.Add($"{(_overruleIsolineDraw == null ? "Включить" : "Отключить")}");
         optKeywords.Keywords.Add($"{(_overruleIsolineDraw == null ? "Разморозить" : "Заморозить")}");

         var resPrompt = ed.GetKeywords(optKeywords);

         if (resPrompt.Status == PromptStatus.OK)
         {
            if (resPrompt.StringResult == "Включить")
            {
               IsolinesOn();
            }
            else if (resPrompt.StringResult == "Отключить")
            {
               IsolinesOff();
            }
            else if (resPrompt.StringResult == "Разморозить")
            {
               // Удалить отдельные штрихи
               Isoline.UnfreezeAll();
               // Включить изолинии
               IsolinesOn();
            }
            else if (resPrompt.StringResult == "Заморозить")
            {
               // Превратить все штрихи в отдельные линии
               Isoline.FreezeAll();
               // выключение изолиний
               IsolinesOff();
            }
            Application.DocumentManager.MdiActiveDocument.Editor.Regen();
         }
      }

      private static void IsolinesOn()
      {
         ContextMenuIsoline.Attach();
         if (_overruleIsolineDraw == null)
         {
            _overruleIsolineDraw = new IsolineDrawableOverrule();
            Overrule.AddOverrule(RXClass.GetClass(typeof(Polyline)), _overruleIsolineDraw, false);
         }
         if (_overruleIsolineTrans == null)
         {
            _overruleIsolineTrans = new IsolineTransformOverrule();
            Overrule.AddOverrule(RXClass.GetClass(typeof(Polyline)), _overruleIsolineTrans, false);
         }
      }

      private static void IsolinesOff()
      {
         ContextMenuIsoline.Detach();
         if (_overruleIsolineDraw != null)
         {
            Overrule.RemoveOverrule(RXClass.GetClass(typeof(Polyline)), _overruleIsolineDraw);
            _overruleIsolineDraw = null;
         }
         if (_overruleIsolineTrans != null)
         {
            Overrule.RemoveOverrule(RXClass.GetClass(typeof(Polyline)), _overruleIsolineTrans);
            _overruleIsolineTrans = null;
         }
      }
   }
}
