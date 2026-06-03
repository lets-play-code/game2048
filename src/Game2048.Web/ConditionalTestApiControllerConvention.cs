using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Game2048.Web;

internal sealed class ConditionalTestApiControllerConvention : IControllerModelConvention
{
    private readonly bool enableTestApi;

    public ConditionalTestApiControllerConvention(bool enableTestApi)
    {
        this.enableTestApi = enableTestApi;
    }

    public void Apply(ControllerModel controller)
    {
        if (enableTestApi || controller.ControllerType.AsType() != typeof(Game2048TestApiController))
        {
            return;
        }

        controller.ApiExplorer.IsVisible = false;
        controller.Selectors.Clear();
        foreach (ActionModel action in controller.Actions)
        {
            action.Selectors.Clear();
        }
    }
}
