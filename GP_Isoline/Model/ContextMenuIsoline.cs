using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;

namespace GP_Isoline.Model
{
   public class ContextMenuIsoline
   {
      private static ContextMenuExtension cme;

      public static void Attach()
      {
         cme = new ContextMenuExtension();
         cme.Popup += cme_Popup;
                  
         RXClass rxc = Entity.GetClass(typeof(Curve));
         Application.AddObjectContextMenuExtension(rxc, cme);
      }

      public static void Detach()
      {
         RXClass rxc = Entity.GetClass(typeof(Curve));
         Application.RemoveObjectContextMenuExtension(rxc, cme);
      }

      private static void ActivateIsolines(object sender, EventArgs e)
      {
         Isoline.ActivateIsolines(true);
      }

      private static void cme_Popup(object sender, EventArgs e)
      {
         ContextMenuExtension cme = (ContextMenuExtension)sender;
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Editor ed = doc.Editor;
         PromptSelectionResult selImplRes = ed.SelectImplied();

         cme.MenuItems.Clear();

         if (selImplRes.Status != PromptStatus.OK)
         {
            return;
         }
         var selIds = selImplRes.Value.GetObjectIds();
         if (selIds.Count() > 0)
         {
            MenuItem miOn = new MenuItem("Включить Бергштрихи");
            miOn.Click += ActivateIsolines;
            cme.MenuItems.Add(miOn);

            MenuItem miOff = new MenuItem("Отключить Бергштрихи");
            miOff.Click += UnActivateIsolines;
            cme.MenuItems.Add(miOff);

            MenuItem miReverse = new MenuItem("Отразить Бергштрихи");
            miReverse.Click += ReverseIsolines;
            cme.MenuItems.Add(miReverse);
         }         
      }

      private static void ReverseIsolines(object sender, EventArgs e)
      {
         Isoline.ReverseIsolines();
      }

      private static void UnActivateIsolines(object sender, EventArgs e)
      {
         Isoline.ActivateIsolines(false);
      }
   }
}