namespace Remembrance.Contracts
{
    public interface IDialogProvider
    {
        bool? ShowDialog();
    }

    public interface ISaveFileDialogProvider : IDialogProvider
    {
        string FileName { get; set; }
    }

    public interface IOpenFileDialogProvider : IDialogProvider
    {
        string FileName { get; }
    }
}
