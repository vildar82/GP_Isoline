using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace GP_Isoline.Model
{
   public class Isoline
   {
      public const string RegAppNAME = "GP-Isoline";
      public ObjectId IdPolyline { get; private set; }
      public bool IsIsoline { get; private set; }
      public bool IsNegate { get; private set; }

      public Isoline(Polyline pl)
      {
         IdPolyline = pl.Id;
         getIsolineProperties(pl);
      }

      public Isoline(ObjectId id)
      {
         IdPolyline = id;
         using (var pl = id.Open(OpenMode.ForRead, false, true) as Polyline)
         {
            getIsolineProperties(pl);
         }
      }

      public static void FreezeAll()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         using (doc.LockDocument())
         {
            Database db = doc.Database;
            using (var t = db.TransactionManager.StartTransaction())
            {
               var bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
               foreach (var idBtr in bt)
               {
                  var btr = idBtr.GetObject(OpenMode.ForRead) as BlockTableRecord;

                  List<Line> linesInBtr = new List<Line>();
                  foreach (var idEnt in btr)
                  {
                     if (idEnt.ObjectClass.Name == "AcDbPolyline")
                     {
                        var pl = idEnt.GetObject(OpenMode.ForRead, false, true) as Polyline;
                        Isoline isoline = new Isoline(pl);
                        if (isoline.IsIsoline)
                        {
                           var lines = isoline.GetLines(pl);
                           linesInBtr.AddRange(lines);
                        }
                     }
                  }
                  if (linesInBtr.Count > 0)
                  {
                     btr.UpgradeOpen();
                     foreach (var line in linesInBtr)
                     {
                        SetLineXData(line);
                        btr.AppendEntity(line);
                        t.AddNewlyCreatedDBObject(line, true);
                     }
                  }
               }
               t.Commit();
            }
         }
      }

      public static void RegAppIsoline()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Editor ed = doc.Editor;
         Database db = doc.Database;

         using (var t = doc.TransactionManager.StartTransaction())
         {
            RegAppTable rat = (RegAppTable)t.GetObject(db.RegAppTableId, OpenMode.ForRead, false);
            if (!rat.Has(RegAppNAME))
            {
               rat.UpgradeOpen();
               RegAppTableRecord ratr = new RegAppTableRecord();
               ratr.Name = RegAppNAME;
               rat.Add(ratr);
               t.AddNewlyCreatedDBObject(ratr, true);
            }
            t.Commit();
         }
      }

      public static void UnfreezeAll()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         using (doc.LockDocument())
         {
            Database db = doc.Database;
            using (var t = db.TransactionManager.StartTransaction())
            {
               var bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
               foreach (var idBtr in bt)
               {
                  var btr = idBtr.GetObject(OpenMode.ForRead) as BlockTableRecord;
                  List<Line> linesInBtr = new List<Line>();
                  foreach (var idEnt in btr)
                  {
                     if (idEnt.ObjectClass.Name == "AcDbLine")
                     {
                        var line = idEnt.GetObject(OpenMode.ForRead, false, true) as Line;
                        if (line.GetXDataForApplication(RegAppNAME) != null)
                        {
                           line.UpgradeOpen();
                           line.Erase();
                        }
                     }
                  }
               }
               t.Commit();
            }
         }
      }

      /// <summary>
      /// Включение бергштрихов для полилинии - запись xdata
      /// Должна быть запущена транзакция!!!
      /// </summary>
      public void Activate()
      {
         using (var pl = IdPolyline.GetObject(OpenMode.ForRead, false, true) as Polyline)
         {
            ResultBuffer rb = pl.GetXDataForApplication(RegAppNAME);
            if (rb != null)
            {
               return;
            }
            using (rb = new ResultBuffer(new TypedValue(1001, RegAppNAME),
                                         new TypedValue((int)DxfCode.ExtendedDataInteger16, 0)))
            {
               pl.UpgradeOpen();
               pl.XData = rb;
            }
         }
      }

      public List<Line> GetLines(Polyline pl)
      {
         List<Line> lines = new List<Line>();
         for (int i = 0; i < pl.NumberOfVertices; i++)
         {
            SegmentType segmentType = pl.GetSegmentType(i);
            if (segmentType == SegmentType.Line)
            {
               var lineSegment = pl.GetLineSegmentAt(i);

               Vector3d vectorIsoline = lineSegment.Direction.GetPerpendicularVector() * 5;
               if (IsNegate)
               {
                  vectorIsoline = vectorIsoline.Negate();
               }
               Point3d ptEndIsoline = lineSegment.MidPoint + vectorIsoline;
               Line line = new Line(lineSegment.MidPoint, ptEndIsoline);
               line.Layer = pl.Layer;
               line.LineWeight = pl.LineWeight;
               line.Color = pl.Color;
               line.Linetype = SymbolUtilityServices.LinetypeContinuousName;
               lines.Add(line);
            }
         }
         return lines;
      }

      /// <summary>
      /// обратить бергштрихи у полилинии
      /// Должна быть запущена транзакция!!!
      /// </summary>
      public void ReverseIsoline()
      {
         using (var pl = IdPolyline.GetObject(OpenMode.ForWrite, false, true) as Polyline)
         {
            ResultBuffer rb = pl.GetXDataForApplication(RegAppNAME);
            if (rb != null)
            {
               var data = rb.AsArray();
               bool isFound = false;
               for (int i = 0; i < data.Length; i++)
               {
                  var tv = data[i];
                  if (tv.TypeCode == (short)DxfCode.ExtendedDataInteger16)
                  {
                     data[i] = new TypedValue((int)DxfCode.ExtendedDataInteger16, IsNegate ? 0 : 1);
                     IsNegate = !IsNegate;
                     isFound = true;
                     break;
                  }
               }
               if (isFound)
               {
                  using (ResultBuffer rbNew = new ResultBuffer(data))
                  {
                     pl.XData = rbNew;
                  }
               }
            }
         }
      }

      private static void SetLineXData(Line line)
      {
         using (ResultBuffer rb = new ResultBuffer(new TypedValue(1001, RegAppNAME),
                                      new TypedValue((int)DxfCode.ExtendedDataInteger16, 0)))
         {
            line.XData = rb;
         }
      }

      private void getIsolineProperties(Polyline pl)
      {
         ResultBuffer rb = pl.GetXDataForApplication(RegAppNAME);
         if (rb != null)
         {
            foreach (var item in rb)
            {
               if (item.TypeCode == (short)DxfCode.ExtendedDataInteger16)
               {
                  IsNegate = Convert.ToBoolean(item.Value);
               }
            }
         }
         IsIsoline = (rb != null);
      }
   }
}