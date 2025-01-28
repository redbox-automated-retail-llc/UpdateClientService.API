using System;

namespace UpdateClientService.API.Services.Transfer
{
    [Flags]
    internal enum FILE_ACL_FLAGS
    {
        BG_COPY_FILE_OWNER = 1,
        BG_COPY_FILE_GROUP = 2,
        BG_COPY_FILE_DACL = 4,
        BG_COPY_FILE_SACL = 8,
        BG_COPY_FILE_ALL = 21, // 0x00000015
    }
}
