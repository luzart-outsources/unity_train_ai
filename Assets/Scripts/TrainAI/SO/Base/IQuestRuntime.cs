namespace TrainAI.SO.Base
{
    public interface IQuestRuntime
    {
        void Begin();
        void Tick(float dt);
        void OnComplete(bool success);
    }
}
