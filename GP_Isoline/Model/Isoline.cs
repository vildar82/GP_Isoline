using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace GP_Isoline.Model
{
   public class Isoline
   {
      public static RXClass RxCurve = RXClass.GetClass(typeof(Curve));
      public const string RegAppNAME = "GP-Isoline";
      public ObjectId IdCurve { get; private set; }
      /// <summary>
      /// Это линия для бергштрихов
      /// </summary>
      public bool IsIsoline { get; private set; }
      /// <summary>
      /// Это линия бергштриха
      /// </summary>
      public bool IsDash { get; private set; }
      public bool IsNegate { get; private set; }

      public Isoline(Curve curve)
      {
         IdCurve = curve.Id;
         getIsolineProperties(curve);
      }

      public Isoline(ObjectId id)
      {
         IdCurve = id;
         using (var curve = id.Open(OpenMode.ForRead, false, true) as Curve)
         {
            getIsolineProperties(curve);
         }
      }

      /// <summary>
      /// Превратить все штрихи в отдельные линии
      /// </summary>
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
                     if (idEnt.ObjectClass.IsDerivedFrom(RxCurve))
                     {
                        var curve = idEnt.GetObject(OpenMode.ForRead, false, true) as Curve;
                        Isoline isoline = new Isoline(curve);
                        if (isoline.IsIsoline)
                        {
                           var lines = isoline.GetLines(curve);
                           linesInBtr.AddRange(lines);
                        }
                     }
                  }
                  if (linesInBtr.Count > 0)
                  {
                     btr.UpgradeOpen();
                     foreach (var line in linesInBtr)
                     {
                        // Пометить линию как бергштрих
                        SetDashXData(line);
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

      public static void RemoveXData(DBObject dbo)
      {
         if (dbo.GetXDataForApplication(RegAppNAME)!=null)
         {
            ResultBuffer rb = rb = new ResultBuffer(new TypedValue(1001, RegAppNAME));
            dbo.UpgradeOpen();
            dbo.XData = rb;
            dbo.DowngradeOpen();
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
                        Isoline isoline = new Isoline(line);                        
                        // Если это штрих, то удвляем ее, она автоматом построится при overrule
                        if (isoline.IsDash)
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

      public static void ActivateIsolines(bool activate)
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
                        if (item.ObjectClass.IsDerivedFrom(Isoline.RxCurve))
                        {
                           Isoline isoline = new Isoline(item);
                           // Активация изолинии
                           if (activate)
                           {
                              if (!isoline.IsIsoline)
                              {
                                 isoline.Activate(true);
                              }
                           }
                           // Отключение изолинии
                           else
                           {
                              if (isoline.IsIsoline)
                              {
                                 isoline.Activate(false);
                              }
                           }
                        }
                     }
                     t.Commit();
                  }
               }
            }
         }
      }

      /// <summary>
      /// Включение/отключение бергштрихов для полилинии - запись xdata
      /// Должна быть запущена транзакция!!!
      /// </summary>
      public void Activate(bool activate)
      {
         using (var curve = IdCurve.GetObject(OpenMode.ForRead, false, true) as Curve)
         {
            //ResultBuffer rb = curve.GetXDataForApplication(RegAppNAME);
            ResultBuffer rb;
            if (activate)
            {
               rb = new ResultBuffer(new TypedValue(1001, RegAppNAME),
                                         // 0 - прямой штрих, 1 - обратный
                                         new TypedValue((int)DxfCode.ExtendedDataInteger16, 0),
                                         // 0 - изолиния, 1 - бергштрих
                                         new TypedValue((int)DxfCode.ExtendedDataInteger16, 0)
                                     );
            }
            else
            {
               rb = new ResultBuffer(new TypedValue(1001, RegAppNAME));
            }            
            curve.UpgradeOpen();
            curve.XData = rb;
         }
      }      

      public List<Line> GetLines(Curve curve)
      {
         List<Line> lines = new List<Line>();
         if (curve is Polyline)
         {
            Polyline pl =(Polyline)curve;
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
               SegmentType segmentType = pl.GetSegmentType(i);
               if (segmentType == SegmentType.Line)
               {
                  var lineSegment = pl.GetLineSegmentAt(i);
                  var line = getLine(lineSegment, curve);
                  lines.Add(line);
               }
            }
         }
         else if (curve is Line)
         {
            Line lineCurve = (Line)curve;
            LineSegment3d segment = new LineSegment3d(lineCurve.StartPoint, lineCurve.EndPoint);
            var line = getLine(segment, curve);
            lines.Add(line);
         }
         return lines;
      }

      private Line getLine(LineSegment3d segment, Curve curve)
      {
         Vector3d vectorIsoline = segment.Direction.GetPerpendicularVector() * Commands.Options.DashLength;
         if (IsNegate)
         {
            vectorIsoline = vectorIsoline.Negate();
         }
         Point3d ptEndIsoline = segment.MidPoint + vectorIsoline;
         Line line = new Line(segment.MidPoint, ptEndIsoline);
         line.Layer = curve.Layer;
         line.LineWeight = curve.LineWeight;
         line.Color = curve.Color;
         line.Linetype = SymbolUtilityServices.LinetypeContinuousName;
         return line;
      }

      /// <summary>
      /// обратить бергштрихи у полилинии
      /// Должна быть запущена транзакция!!!
      /// </summary>
      public void ReverseIsoline()
      {
         using (var curve = IdCurve.GetObject(OpenMode.ForWrite, false, true) as Curve)
         {
            ResultBuffer rb = curve.GetXDataForApplication(RegAppNAME);
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
                     curve.XData = rbNew;
                  }
               }
            }
         }
      }

      private static void SetDashXData(Line line)
      {
         using (ResultBuffer rb = new ResultBuffer(new TypedValue(1001, RegAppNAME),
                                      new TypedValue((int)DxfCode.ExtendedDataInteger16, 0),
                                      new TypedValue((int)DxfCode.ExtendedDataInteger16, 1)))                                      
         {
            line.XData = rb;
         }
      }

      private void getIsolineProperties(Curve curve)
      {
         ResultBuffer rb = curve.GetXDataForApplication(RegAppNAME);
         if (rb != null)
         {
            TypedValue[] tvs = rb.AsArray();
            int countTvs = tvs.Count();
            if (countTvs>1)
            {
               TypedValue tvNegate = tvs[1];
               if (tvNegate.TypeCode == (short)DxfCode.ExtendedDataInteger16)
               {
                  IsNegate = Convert.ToBoolean(tvNegate.Value);
               }
            }
            if (countTvs>2)
            {
               TypedValue tvTypeIsoline = tvs[2];
               if (tvTypeIsoline.TypeCode == (short)DxfCode.ExtendedDataInteger16)
               {
                  IsDash = Convert.ToBoolean(tvTypeIsoline.Value);
               }
            }
            IsIsoline = !IsDash;     
         }         
      }
   }
}