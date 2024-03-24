using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Models.View;

public delegate Task IncreaseProgressAmount(double amount, string message = "");

public record ProgressUpdateActions(Func<string, Task> UpdateProgressText, IncreaseProgressAmount IncreaseAmount);