using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssassin;

public enum SpamAssassinStatus
{
    OK = 0,
    Usage = 64,
    DataError = 65,
    NoInput = 66,
    NoUser = 67,
    NoHost = 68,
    Unavailable = 69,
    Software = 70,
    OSError = 71,
    OSFile = 72,
    CantCreate = 73,
    IOError = 74,
    TemporaryFail = 75,
    Protocol = 76,
    NoPermission = 77,
    Config = 78,
    Timeout = 79
}