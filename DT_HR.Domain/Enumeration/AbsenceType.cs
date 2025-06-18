using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Enumeration;

public sealed class AbsenceType : Enumeration<AbsenceType>
{
    
    public static readonly AbsenceType Absent = new AbsenceType(0, "Absent");
    public static readonly AbsenceType OnTheWay = new AbsenceType(1, "OnTheWay");
    public static readonly AbsenceType Overslept = new AbsenceType(2, "Overslept");
    public static readonly AbsenceType Custom = new AbsenceType(3, "Custom");

    
    private AbsenceType(int value, string name): base(value,name) { }

}