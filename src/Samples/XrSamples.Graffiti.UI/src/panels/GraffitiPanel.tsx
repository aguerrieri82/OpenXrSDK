import { forModel } from "@eusoft/webapp-jsx";
import { formatText, MaterialIcon } from "@eusoft/webapp-ui";
import { BasePanel } from "./BasePanel";
import { appBridge } from "../services/Bridge";
import { IGraffitiInfo } from "../entities/Types";
import "./GraffitiPanel.scss";

interface IGraffitiView {
    info: IGraffitiInfo;
    name: string;
    thumbnail: string;
}

export class GraffitiPanel extends BasePanel {

    constructor() {

        super();

        this.init(GraffitiPanel);

        this.body = forModel(this, m =>
            <div className="graffiti-panel">

                <div className="graffiti-list">

                    {m.items.forEach(item =>
                        <div className="graffiti-item ui-item">

                            <img className="graffiti-thumb" src={item.thumbnail} />

                            <div className="graffiti-name">
                                {item.name}
                            </div>

                            <div className="graffiti-actions">

                                <button className="ui-button ghost" title={formatText("load-original") as string}
                                    on-click={() => appBridge.loadGraffitiAsync(item.info.id, true)}>
                                    <MaterialIcon name="my_location" />
                                </button>

                                <button className="ui-button ghost" title={formatText("load-place") as string}
                                    on-click={() => appBridge.loadGraffitiAsync(item.info.id, false)}>
                                    <MaterialIcon name="ads_click" />
                                </button>

                                <button className="ui-button ghost danger" title={formatText("delete") as string}
                                    on-click={() => m.deleteAsync(item)}>
                                    <MaterialIcon name="delete" />
                                </button>

                            </div>

                        </div>
                    )}

                    {m.items.length == 0 &&
                        <div className="ui-empty">
                            <span>{formatText("no-graffiti")}</span>
                        </div>
                    }

                </div>

                <div className="graffiti-footer">

                    <button className="ui-button" on-click={() => appBridge.clearCanvasAsync()}>
                        <MaterialIcon name="delete_sweep" />
                        <span>{formatText("clear")}</span>
                    </button>

                    <button className="ui-button" on-click={() => appBridge.newGraffitiAsync()}>
                        <MaterialIcon name="note_add" />
                        <span>{formatText("new")}</span>
                    </button>

                    <button className="ui-button" on-click={() => m.saveAsync(true)}>
                        <MaterialIcon name="save_as" />
                        <span>{formatText("save-new")}</span>
                    </button>

                    <button className="save-button ui-button primary" on-click={() => m.saveAsync(false)}>
                        <MaterialIcon name="save" />
                        <span>{formatText("save")}</span>
                    </button>

                </div>

            </div>
        );

        this.title = "graffiti";
    }

    override async refreshAsync() {

        await super.refreshAsync();

        const list = await appBridge.listGraffitiAsync();

        this.items = list.map(info => ({
            info,
            name: `${formatText("graffiti")} ${new Date(info.date).toLocaleString()}`,
            thumbnail: ""
        }));

        for (const item of this.items)
            item.thumbnail = await appBridge.getThumbnailAsync(item.info.id);
    }

    protected async saveAsync(asNew: boolean) {

        await appBridge.saveGraffitiAsync(asNew);

        await this.refreshAsync();
    }

    protected async deleteAsync(item: IGraffitiView) {

        await appBridge.deleteGraffitiAsync(item.info.id);

        await this.refreshAsync();
    }

    items: IGraffitiView[] = [];
}

export default GraffitiPanel;
