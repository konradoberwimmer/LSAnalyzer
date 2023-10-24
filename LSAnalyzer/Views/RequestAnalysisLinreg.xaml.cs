using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LSAnalyzer.Views
{
    /// <summary>
    /// Interaktionslogik für RequestAnalysisLinreg.xaml
    /// </summary>
    public partial class RequestAnalysisLinreg : RequestAnalysisBaseView, IRequestingAnalysis
    {
        public RequestAnalysisLinreg(RequestAnalysis requestAnalysisViewModel)
        {
            InitializeComponent();

            DataContext = requestAnalysisViewModel;
        }

        public Type GetAnalysisType()
        {
            return Type.GetType("LSAnalyzer.Models.AnalysisLinreg")!;
        }
    }
}
