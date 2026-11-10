import { Content, formatText, IContentInfo } from "@eusoft/webapp-ui";
import { Class, forModel } from "@eusoft/webapp-jsx";
import { registerComponent, USE } from "@eusoft/webapp-core";
import { GraffitiPanel } from "../panels";
import type { BasePanel } from "../panels/BasePanel";
import "./MainPage.scss";

interface IPanelInfo {
    panel: BasePanel;
}

export class MainPage extends Content {

    constructor() {

        super();

        this.panels = [
            { panel: new GraffitiPanel() }
        ] as IPanelInfo[];

        this.init(MainPage, {
            name: MainPage.info.name,
            title: "Home",
            style: ["panel"],
            body: forModel(this, m => <>
                <div className="side-bar">
                    {m.panels.forEach(p =>
                        <div on-click={() => m.loadPanelAsync(p.panel)}>
                            <Class condition={p[USE](m).activePanel == p.panel} name="active" />
                            {formatText(p.panel.title)}
                        </div>
                    )}
                </div>
                {m.activePanel}
            </>)
        });
    }

    override async onLoadAsync() {

        await this.loadPanelAsync(this.panels[0].panel);

        return true;
    }

    async loadPanelAsync(panel: BasePanel) {

        if (this.activePanel)
            this.activePanel.isActive = false;

        this.activePanel = panel;

        if (this.activePanel)
            this.activePanel.isActive = true;

        await panel.refreshAsync();
    }

    activePanel: BasePanel;

    panels: IPanelInfo[];

    static override info = {
        name: "main",
        route: "/",
        factory: () => new MainPage()
    } as IContentInfo;
}

export default MainPage;

registerComponent(MainPage, "MainPage");
