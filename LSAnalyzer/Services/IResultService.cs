using LSAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Services;

public interface IResultService
{
    Analysis? Analysis { get; set; }

    DataTable? CreatePrimaryTable();
}
