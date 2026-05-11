namespace Nalu.SharpState.AotTester;

public class DoorMachineTester
{
    public DoorMachineTester()
    {
        var actor = DoorMachine.CreateActor(new DoorContext(), new StateMachineEmptyServiceProviderResolver());
        actor.Open("test");
    }
}
