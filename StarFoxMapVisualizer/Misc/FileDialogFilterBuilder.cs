using System;
using System.Collections.Generic;
using System.Text;

namespace StarFoxMapVisualizer.Misc
{
    internal sealed class FileDialogFilterBuilder
    {
	    private IDictionary<string, string> m_dicFilters;

	    public bool Sorted => m_dicFilters is SortedList<string, string>;

	    public bool IncludeAllSupported { get; set; }

	    public bool IncludeAllFiles { get; set; }

	    public FileDialogFilterBuilder(bool sorted)
	    {
		    if (sorted) {
			    m_dicFilters = new SortedList<string, string>();
		    } else {
			    m_dicFilters = new Dictionary<string, string>();
			}
	    }

	    public void Add(string displayText, string extensions)
	    {
		    if (String.IsNullOrEmpty(extensions)) {
			    throw new ArgumentNullException(nameof(extensions));
		    }

		    if (extensions.StartsWith(".")) {
			    extensions = "*" + extensions;
		    } else if (!extensions.StartsWith("*.")) {
			    extensions = "*." + extensions;
		    }
			m_dicFilters.Add(displayText, extensions);
	    }

	    public override string ToString()
	    {
		    var stbFilter = new StringBuilder();
		    string strExt;
		    List<string> lstExt = new List<string>(m_dicFilters.Count);
			foreach (var kvp in m_dicFilters) {
				strExt = kvp.Value;
				lstExt.Add(strExt);
				stbFilter.Append(kvp.Key).Append(" (").Append(strExt).Append(")|").Append(strExt).Append('|');
			}

		    if (this.IncludeAllSupported) {
			    strExt = String.Join(", ", lstExt.ToArray());
			    stbFilter.Append("All supported formats (").Append(strExt).Append(")|").Append(strExt).Append('|');
		    }
		    if (this.IncludeAllFiles) {
			    stbFilter.Append("All files (*.*)|*.*|");
		    }

		    stbFilter.Length--;
		    return stbFilter.ToString();
	    }
    }
}
