public interface IWorldInteractionUI
{

    void OpenNpcDialog(ZoneData npc, string startKnot);
    void OpenWorldObject(ZoneData worldObj, string startKnot);
    void OpenZoneTravel(ZoneData zone); // or OpenZoneTravel(string zoneId)
}