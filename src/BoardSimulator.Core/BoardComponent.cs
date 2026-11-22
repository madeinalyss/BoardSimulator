using System;
using System.Text.RegularExpressions;

namespace BoardSimulator.Core
{
    public abstract class BoardComponent
    {
        public string Id { get; }
        public string DisplayName { get; }

        protected BoardComponent(string id, string displayName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            if (!Regex.IsMatch(id, @"\A[A-Za-z][A-Za-z0-9_]*\z"))
                throw new ArgumentException("Invalid component ID.", nameof(id));

            Id = id;
            DisplayName = displayName;
        }
    }
}
