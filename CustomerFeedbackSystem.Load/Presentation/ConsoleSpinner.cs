namespace CustomerFeedbackSystem.Load.Presentation;

/// <summary>
/// Animated "working..." indicator on the current console line while an
/// awaited operation runs. No-ops when output is redirected (a log file has
/// no use for spinner frames) so it never corrupts a captured Task Scheduler
/// log.
/// </summary>
internal sealed class ConsoleSpinner : IAsyncDisposable
{
    private static readonly char[] Frames = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];

    private readonly string _message;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task? _animation;

    public ConsoleSpinner(string message)
    {
        _message = message;
        _animation = Console.IsOutputRedirected ? null : Task.Run(() => AnimateAsync(_cts.Token));
    }

    private async Task AnimateAsync(CancellationToken cancellationToken)
    {
        var frame = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"\r{Frames[frame % Frames.Length]} {_message}");
            frame++;

            try
            {
                await Task.Delay(90, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Expected on Dispose; the loop exits on the next check.
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_animation is null)
        {
            return;
        }

        await _cts.CancelAsync();

        try
        {
            await _animation;
        }
        catch (OperationCanceledException)
        {
            // Expected.
        }

        Console.Write("\r" + new string(' ', _message.Length + 2) + "\r");
    }
}
