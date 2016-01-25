using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
         //MenuItem mi = new MenuItem("ГП-Бергштрихи");
         //mi.Click += PrintHello;
         //cme.MenuItems.Add(mi);
         RXClass rxc = Entity.GetClass(typeof(Polyline));
         Application.AddObjectContextMenuExtension(rxc, cme);
      }

      private static void PrintHello(object sender, EventArgs e)
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Editor ed = doc.Editor;
         ed.WriteMessage("Hello Isolines");
      }

      static void cme_Popup(object sender, EventArgs e)
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

            MenuItem miReverse = new MenuItem("Отразить Бергштрихи");
            miReverse.Click += ReverseIsolines;
            cme.MenuItems.Add(miReverse);
         }       

         // Первый вариант: управление доступностью элементов без их удаления/добавления.
         //foreach (MenuItem item in cme.MenuItems)
         //    item.Enabled = isEnadled;

         //// Второй вариант: динамическое удаление/добавление элементов меню.
         //if (isMenuEnadled && cme.MenuItems.Count == 0)
         //{
         //   MenuItem mi = new MenuItem("My AcDbPolyline menu item");
         //   mi.Click += new EventHandler(PrintHello);
         //   cme.MenuItems.Add(mi);
         //}
         //else if (!isMenuEnadled)
         //   foreach (MenuItem item in cme.MenuItems.ToArray())
         //   {
         //      item.Click -= new EventHandler(PrintHello);
         //      cme.MenuItems.Remove(item);
         //   }

      }

      private static void ReverseIsolines(object sender, EventArgs e)
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         using (doc.LockDocument())
         {
            Editor ed = doc.Editor;
            PromptSelectionResult result = ed.SelectImplied();
            if (result.Status == PromptStatus.OK)
            {
               var selIds = result.Value.GetObjectIds();
               if (selIds.Count() > 0)
               {
                  using (var t = doc.Database.TransactionManager.StartTransaction())
                  {
                     foreach (var item in selIds)
                     {
                        if (item.ObjectClass.Name == "AcDbPolyline")
                        {
                           Isoline isoline = new Isoline(item);
                           if (isoline.IsIsoline)
                           {
                              isoline.ReverseIsoline();
                           }
                        }
                     }
                     t.Commit();
                  }
               }
            }
         }
      }

      private static void ActivateIsolines(object sender, EventArgs e)
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         using (doc.LockDocument())
         {
            Editor ed = doc.Editor;
            PromptSelectionResult result = ed.SelectImplied();
            if (result.Status == PromptStatus.OK)
            {
               var selIds = result.Value.GetObjectIds();
               if (selIds.Count() > 0)
               {
                  using (var t = doc.Database.TransactionManager.StartTransaction())
                  {
                     foreach (var item in selIds)
                     {
                        if (item.ObjectClass.Name == "AcDbPolyline")
                        {
                           Isoline isoline = new Isoline(item);
                           if (!isoline.IsIsoline)
                           {
                              isoline.Activate();
                           }
                        }
                     }
                     t.Commit();
                  }
               }
            }
         }
      }

      public static void Detach()
      {
         RXClass rxc = Entity.GetClass(typeof(Polyline));
         Application.RemoveObjectContextMenuExtension(rxc, cme);
      }
   }
}
