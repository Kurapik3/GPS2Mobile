public interface ISubAI
{
    void Initialize(IAIContext context, IAIActor actor);
    void Execute(); //Called each turn by AIController
    int Priority { get; } //Used to have AI priority sorting
}
