using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace PdkOcrClient;

public class CommitTextBoxEditBehavior : Behavior<TextBox>
{
    public static readonly StyledProperty<bool> IsEditingProperty =
        AvaloniaProperty.Register<CommitTextBoxEditBehavior, bool>(nameof(IsEditing));

    public static readonly StyledProperty<ICommand?> CompleteCommandProperty =
        AvaloniaProperty.Register<CommitTextBoxEditBehavior, ICommand?>(nameof(CompleteCommand));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<CommitTextBoxEditBehavior, object?>(nameof(CommandParameter));

    public bool IsEditing
    {
        get => GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    public ICommand? CompleteCommand
    {
        get => GetValue(CompleteCommandProperty);
        set => SetValue(CompleteCommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is null) return;

        AssociatedObject.KeyDown += OnKeyDown;
        AssociatedObject.LostFocus += OnLostFocus;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is null) return;

        AssociatedObject.KeyDown -= OnKeyDown;
        AssociatedObject.LostFocus -= OnLostFocus;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsEditingProperty && IsEditing)
        {
            FocusEditor();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !IsEditing)
        {
            return;
        }

        CompleteEdit();
        e.Handled = true;
    }

    private void OnLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (IsEditing)
        {
            CompleteEdit();
        }
    }

    private void FocusEditor()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (AssociatedObject is null || !IsEditing)
            {
                return;
            }

            AssociatedObject.Focus();
            AssociatedObject.SelectAll();
        });
    }

    private void CompleteEdit()
    {
        var command = CompleteCommand;
        var parameter = CommandParameter;

        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (AssociatedObject is null)
                return;

            /// ListBox 의 경우 다시 포커스를 되돌려서 위 아래 방향키로 다음 것을 수정할 수 있도록 한다.
            var listBoxItem = AssociatedObject.FindAncestorOfType<ListBoxItem>();
            listBoxItem?.Focus();
        });
    }
}
