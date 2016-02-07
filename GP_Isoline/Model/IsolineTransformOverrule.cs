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
         Curve curve = entity as Curve;
         if (curve != null)
         {
            Isoline isoline = new Isoline(curve);
            if (isoline.IsIsoline)
            {
               var lines = isoline.GetLines(curve);
               foreach (var line in lines)
               {
                  entitySet.Add(line);
               }
               //isoline.Activate(false); fatal
               Isoline.RemoveXData(entity);
            }
         }
         if (entity is Line)
         {
            entitySet.Add((DBObject)entity.Clone());
            return;
         }
         base.Explode(entity, entitySet);
      }
   }
}