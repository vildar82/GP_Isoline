using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GP_Isoline.Model
{
   public partial class FormIsolineOptions : Form
   {
      public IsolineOptions IsolineOptions { get; set; }

      public FormIsolineOptions(IsolineOptions isolineOptions)
      {
         InitializeComponent();
         IsolineOptions = isolineOptions;
         propertyGrid1.SelectedObject = IsolineOptions;
      }
   }
}
