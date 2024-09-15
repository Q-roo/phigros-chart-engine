namespace PCE.Chartbuild;

public enum WarningType
{
    MissingBlock, // if(true);
    DeadCode, // {...return; let a;}
}