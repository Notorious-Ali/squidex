/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */


import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ActionsDto, ConfirmClickDirective, DropdownMenuComponent, DynamicRuleDto, DynamicUpdateRuleDto, EditableTitleComponent, ModalDirective, ModalModel, ModalPlacementDirective, RulesState, ToggleComponent, TranslatePipe, TriggersDto } from '@app/shared';
import { RuleElementComponent } from '../../shared/rule-element.component';

@Component({
    standalone: true,
    selector: 'sqx-rule',
    styleUrls: ['./rule.component.scss'],
    templateUrl: './rule.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ConfirmClickDirective,
        DropdownMenuComponent,
        EditableTitleComponent,
        FormsModule,
        ModalDirective,
        ModalPlacementDirective,
        RouterLink,
        RuleElementComponent,
        ToggleComponent,
        TranslatePipe,
    ],
})
export class RuleComponent {
    @Input({ required: true })
    public ruleTriggers!: TriggersDto;

    @Input({ required: true })
    public ruleActions!: ActionsDto;

    @Input({ required: true })
    public rule!: DynamicRuleDto;

    public dropdown = new ModalModel();

    public get isManual() {
        return this.rule.trigger.triggerType === 'Manual';
    }

    constructor(
        private readonly rulesState: RulesState,
    ) {
    }

    public delete() {
        this.rulesState.delete(this.rule);
    }

    public run() {
        this.rulesState.run(this.rule);
    }

    public runFromSnapshots() {
        this.rulesState.runFromSnapshots(this.rule);
    }

    public rename(name: string) {
        this.rulesState.update(this.rule, new DynamicUpdateRuleDto({ name }));
    }

    public disable() {
        this.rulesState.update(this.rule, new DynamicUpdateRuleDto({ isEnabled: false }));
    }

    public enable() {
        this.rulesState.update(this.rule, new DynamicUpdateRuleDto({ isEnabled: true }));
    }

    public toggle() {
        this.rulesState.update(this.rule, new DynamicUpdateRuleDto({ isEnabled: !this.rule.isEnabled }));
    }

    public trigger() {
        this.rulesState.trigger(this.rule);
    }
}
