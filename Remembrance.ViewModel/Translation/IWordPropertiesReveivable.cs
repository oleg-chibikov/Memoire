using System;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.Events;

namespace Remembrance.ViewModel.Translation
{
    public interface IWordPropertiesReveivable
    {
        string Text { get; }
        event EventHandler<EventArgs<string>> ParentTextSet;
        event EventHandler<EventArgs<WordKey>> WordKeySet;
    }
}