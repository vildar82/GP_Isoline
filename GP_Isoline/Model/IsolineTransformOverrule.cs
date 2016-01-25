using Autodesk.AutoCAD.DatabaseServices;

namespace GP_Isoline.Model
{
   public class IsolineTransformOverrule : TransformOverrule
   {
      public IsolineTransformOverrule()
      {
         SetXDataFilter(Isoline.RegAppNAME);
      }

      public override void Explode(Entity entity, DBObjectCollection entitySet)
      {
         base.Explode(entity, entitySet);
         Polyline pl = entity as Polyline;
         if (pl != null)
         {
            Isoline isoline = new Isoline(pl);
            if (isoline.IsIsoline)
            {
               var lines = isoline.GetLines(pl);
               foreach (var line in lines)
               {
                  entitySet.Add(line);
               }
            }
         }
      }
   }
}