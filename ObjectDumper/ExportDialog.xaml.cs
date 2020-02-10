using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ObjectDumper
{
    /// <summary>
    /// Interaction logic for WpfDialog.xaml
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "Done in constructor.")]
    public partial class ExportDialog : System.Windows.Window
    {
        public ExportDialog(List<EnvDTE.Expression> locals)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            InitializeComponent();
            Locals = locals;
            PopulateDropDown(locals);
        }

        private List<EnvDTE.Expression> Locals { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LocalDropDown_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                OutPut.Text = "...GENERATING...";
                OutPut.TextAlignment = TextAlignment.Center;
                var dropDown = sender as ComboBox;
                var selectedLocal = Locals.FirstOrDefault(i => i.Name == dropDown.SelectedValue.ToString());
                dropDown.IsDropDownOpen = false;
                TypeInfo.Text = selectedLocal.Type.ToString();
                CopyButton.IsEnabled = false;
                GenerateInTaskAsync(selectedLocal);
            }
            catch (Exception ex)
            {
                TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                OutPut.Text = ex.ToString();
            }
        }

        private void GenerateInTaskAsync(EnvDTE.Expression experssion)
        {
            _ = Task.Run(async () =>
              {
                  try
                  {
                      await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                      var generator = new JsonGenerator();
                      var json = generator.GenerateJson(experssion);
                      OutPut.Text = json;
                      OutPut.TextAlignment = TextAlignment.Left;
                      CopyButton.IsEnabled = true;
                  }
                  catch (Exception ex)
                  {
                      TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                      OutPut.Text = ex.ToString();
                  }
              });
        }


        private void PopulateDropDown(List<EnvDTE.Expression> locals)
        {
            try
            {
                Dispatcher.VerifyAccess();
                locals.ForEach(i => LocalDropDown.Items.Add(i.Name));
            }
            catch (Exception ex)
            {
                TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                OutPut.Text = ex.ToString();
            }
        }

        private void CopyToClipBoardButtonClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(OutPut.Text);
        }
    }
}