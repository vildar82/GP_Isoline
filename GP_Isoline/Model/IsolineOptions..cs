using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using AcadLib.Files;
using Autodesk.AutoCAD.ApplicationServices;

namespace GP_Isoline.Model
{
   [Serializable]
   public class IsolineOptions
   {
      private static AcadLib.DictNOD dictNod = new AcadLib.DictNOD("GP_Isoline");
      private static readonly string fileOptions = Path.Combine(
                     AutoCAD_PIK_Manager.Settings.PikSettings.ServerShareSettingsFolder,
                     "ГП\\GP_Isoline.xml");      

      private IsolineOptions()
      {
      }      

      [DisplayName("Длина штриха по умолчанию")]            
      public double DashLengthDefault { get; set; }
      [DisplayName("Длина штриха в этом чертеже")]      
      [XmlIgnore]
      public double DashLength { get; set; }

      public void Save()
      {
         try
         {            
            if (!File.Exists(fileOptions))
            {
               Directory.CreateDirectory(Path.GetDirectoryName(fileOptions));
            }
            SerializerXml xmlSer = new SerializerXml(fileOptions);
            xmlSer.SerializeList(this);
            SaveDrawingOptions();
         }
         catch (Exception ex)
         {
            Logger.Log.Error(ex, "Не удалось сериализовать настройки в {0}", fileOptions);
         }
      }

      private static IsolineOptions DefaultOptions()
      {
         IsolineOptions options = new IsolineOptions();
         options.DashLengthDefault = 5;
         options.DashLength = 5;
         return options;
      }

      public static IsolineOptions Load()
      {
         IsolineOptions options = null;
         // загрузка из файла настроек
         if (File.Exists(fileOptions))
         {
            SerializerXml xmlSer = new SerializerXml(fileOptions);
            try
            {
               options = xmlSer.DeserializeXmlFile<IsolineOptions>();
               if (options != null)
               {
                  options.LoadDrawingOptions();      
                  return options;
               }
            }
            catch (Exception ex)
            {
               Logger.Log.Error(ex, "Не удалось десериализовать настройки из файла {0}", fileOptions);
            }
         }
         return DefaultOptions();
      }

      public IsolineOptions Show()
      {
         IsolineOptions resVal = this;
         FormIsolineOptions formOpt = new FormIsolineOptions((IsolineOptions)this.MemberwiseClone());
         if (Application.ShowModalDialog(formOpt) == System.Windows.Forms.DialogResult.OK)
         {
            resVal = formOpt.IsolineOptions;
            resVal.Save();            
         }
         return resVal;
      }

      private void LoadDrawingOptions()
      {
         DashLength = dictNod.Load("DashLength", DashLengthDefault);
      }
      private void SaveDrawingOptions()
      {
         dictNod.Save(DashLength,"DashLength");
      }
   }
}