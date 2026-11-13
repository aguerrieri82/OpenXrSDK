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
                        <div className="graffiti-item ui-item" on-click={(_, e: Event) => m.loadGraffitiAsync(e, item, true)}>

                            <img className="graffiti-thumb" src={item.thumbnail} />

                            <div className="graffiti-name">
                                {item.name}
                            </div>

                            <div className="graffiti-actions">

                                <button className="ui-button ghost" title={formatText("load-place") as string}
                                    on-click={(_, e: Event) => m.loadGraffitiAsync(e, item, false) }>
                                    <MaterialIcon name="ads_click" />
                                </button>

                                <button className="ui-button ghost" title={formatText("share") as string}
                                    on-click={(_, e: Event) => m.shareAsync(e, item) }>
                                    <MaterialIcon name="share" />
                                </button>

                                <button className="ui-button ghost danger" title={formatText("delete") as string}
                                    on-click={(_, e: Event) =>  m.deleteAsync(e, item)}>
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

    protected async loadGraffitiAsync(ev: Event, item: IGraffitiView, autoPlace: boolean) {

        ev.stopPropagation();
        ev.stopImmediatePropagation();

        await appBridge.loadGraffitiAsync(item.info.id, autoPlace)
    }

    protected async saveAsync(asNew: boolean) {

        await appBridge.saveGraffitiAsync(asNew);

        await this.refreshAsync();
    }

    protected async deleteAsync(ev: Event, item: IGraffitiView) {

        ev.stopPropagation();
        ev.stopImmediatePropagation();

        await appBridge.deleteGraffitiAsync(item.info.id);

        await this.refreshAsync();
    }

    protected async shareAsync(ev: Event, item: IGraffitiView) {

        ev.stopPropagation();
        ev.stopImmediatePropagation();

        await appBridge.shareColorImageAsync(item.info.id);
    }

    items: IGraffitiView[] = [];
}

export default GraffitiPanel;
