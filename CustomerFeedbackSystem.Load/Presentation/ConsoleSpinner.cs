namespace CustomerFeedbackSystem.Load.Presentation;


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
        }

        Console.Write("\r" + new string(' ', _message.Length + 2) + "\r");
    }
}
