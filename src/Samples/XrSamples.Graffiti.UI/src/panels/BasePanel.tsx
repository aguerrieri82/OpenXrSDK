import {  Component } from "@eusoft/webapp-core";
import { forModel } from "@eusoft/webapp-jsx/Helpers";
import { formatText, IAction, createAction } from "@eusoft/webapp-ui";
import { LocalString, ViewNode } from "@eusoft/webapp-ui/Types";
import "./BasePanel.scss";


export abstract class BasePanel extends Component {

    protected _isLoaded = false;    

    constructor() {

        super();

        this.init(BasePanel, {
            template: forModel<this>(m => <div className={m.className}>
                <h2>{formatText(m.title)}</h2>
                <div className="body">
                    {m.body}
                </div>
                {m.actions?.length > 0 && <div className="actions">
                    {m.actions.forEach(a => createAction(a, "contained"))}
                </div>
                }
            </div>)
        });
    }

    loadAsync(): Promise<void> {

        return;
    }


    async refreshAsync() {

        if (!this._isLoaded) {
            await this.loadAsync();
            this._isLoaded = true;  
        }
    }

    actions: IAction[];

    title: LocalString;

    body: ViewNode;

    isActive: boolean;
}


