using ModManager.Models.View;

namespace ModManager.Views.StatsValidator;

public partial class StatsValidatorLineView : ReactiveUserControl<StatsValidatorLineText>
{
	public StatsValidatorLineView()
	{
		InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif
	}
}