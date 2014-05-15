using System.Windows.Forms;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;

namespace Tinker.ReSharper.PropertyChanged
{
	[ActionHandler("Tinker.ReSharper.PropertyChanged.About")]
	public class AboutAction
		: IActionHandler
	{
		public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
		{
			// return true or false to enable/disable this action
			return true;
		}

		public void Execute(IDataContext context, DelegateExecute nextExecute)
		{
			MessageBox.Show(
			  "Tinker.ReSharper.PropertyChanged\nTinker Thomas\n\nHelper actions for INotifyPropertyChanged",
			  "About Tinker.ReSharper.PropertyChanged",
			  MessageBoxButtons.OK,
			  MessageBoxIcon.Information);
		}
	}
}