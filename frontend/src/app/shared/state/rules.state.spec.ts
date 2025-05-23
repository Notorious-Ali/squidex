/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { firstValueFrom, of, onErrorResumeNextWith, throwError } from 'rxjs';
import { IMock, It, Mock, Times } from 'typemoq';
import { DialogService, DynamicCreateRuleDto, DynamicRuleDto, DynamicRulesDto, DynamicUpdateRuleDto, ManualRuleTriggerDto, RulesService, versioned } from '@app/shared/internal';
import { createRule } from '../services/rules.service.spec';
import { TestValues } from './_test-helpers';
import { RulesState } from './rules.state';

describe('RulesState', () => {
    const {
        app,
        appsState,
        newVersion,
    } = TestValues;

    const rule1 = createRule(1);
    const rule2 = createRule(2);

    const newRule = createRule(3);

    let dialogs: IMock<DialogService>;
    let rulesService: IMock<RulesService>;
    let rulesState: RulesState;

    beforeEach(() => {
        dialogs = Mock.ofType<DialogService>();

        rulesService = Mock.ofType<RulesService>();
        rulesState = new RulesState(appsState.object, dialogs.object, rulesService.object);
    });

    afterEach(() => {
        rulesService.verifyAll();
    });

    describe('Loading', () => {
        it('should load rules', () => {
            rulesService.setup(x => x.getRules(app))
                .returns(() => of(new DynamicRulesDto(({ items: [rule1, rule2], runningRuleId: rule1.id, _links: {} })))).verifiable();

            rulesState.load().subscribe();

            expect(rulesState.snapshot.isLoaded).toBeTruthy();
            expect(rulesState.snapshot.isLoading).toBeFalsy();
            expect(rulesState.snapshot.rules).toEqual([rule1, rule2]);

            let ruleRunning: DynamicRuleDto | undefined;
            rulesState.runningRule.subscribe(result => {
                ruleRunning = result;
            });

            expect(ruleRunning).toBe(rule1);
            expect(rulesState.snapshot.runningRuleId).toBe(rule1.id);

            dialogs.verify(x => x.notifyInfo(It.isAnyString()), Times.never());
        });

        it('should reset loading state if loading failed', () => {
            rulesService.setup(x => x.getRules(app))
                .returns(() => throwError(() => 'Service Error'));

            rulesState.load().pipe(onErrorResumeNextWith()).subscribe();

            expect(rulesState.snapshot.isLoading).toBeFalsy();
        });

        it('should show notification on load if reload is true', () => {
            rulesService.setup(x => x.getRules(app))
                .returns(() => of(new DynamicRulesDto(({ items: [rule1, rule2], _links: {} })))).verifiable();

            rulesState.load(true).subscribe();

            expect().nothing();

            dialogs.verify(x => x.notifyInfo(It.isAnyString()), Times.once());
        });
    });

    describe('Updates', () => {
        beforeEach(() => {
            rulesService.setup(x => x.getRules(app))
                .returns(() => of(new DynamicRulesDto(({ items: [rule1, rule2], _links: {} })))).verifiable();

            rulesState.load().subscribe();
        });

        it('should return rule on select and not load if already loaded', async () => {
            const ruleSelected = await firstValueFrom(rulesState.select(rule1.id));

            expect(ruleSelected).toEqual(rule1);
            expect(rulesState.snapshot.selectedRule).toEqual(rule1);
        });

        it('should return null on select if unselecting rule', async () => {
            const ruleSelected = await firstValueFrom(rulesState.select(null));

            expect(ruleSelected).toBeNull();
            expect(rulesState.snapshot.selectedRule).toBeNull();
        });

        it('should add rule to snapshot if created', () => {
            const request = new DynamicCreateRuleDto({
                trigger: new ManualRuleTriggerDto(),
                action: {
                    actionType: 'action3',
                },
            });

            rulesService.setup(x => x.postRule(app, request))
                .returns(() => of(newRule));

            rulesState.create(request).subscribe();

            expect(rulesState.snapshot.rules).toEqual([rule1, rule2, newRule]);
        });

        it('should update rule if updated', () => {
            const request = new DynamicUpdateRuleDto({
                trigger: new ManualRuleTriggerDto(),
                action: {
                    actionType: 'action3',
                },
            });

            const updated = createRule(1, '_new');

            rulesService.setup(x => x.putRule(app, rule1, request, rule1.version))
                .returns(() => of(updated)).verifiable();

            rulesState.update(rule1, request).subscribe();

            expect(rulesState.snapshot.rules).toEqual([updated, rule2]);
        });

        it('should not update rule in snapshot if triggered', () => {
            rulesService.setup(x => x.triggerRule(app, rule1))
                .returns(() => of(true)).verifiable();

            rulesState.trigger(rule1).subscribe();

            expect(rulesState.snapshot.rules).toEqual([rule1, rule2]);
        });

        it('should not update rule in snapshot if running', () => {
            rulesService.setup(x => x.runRule(app, rule1))
                .returns(() => of(true)).verifiable();

            rulesState.run(rule1).subscribe();

            expect(rulesState.snapshot.rules).toEqual([rule1, rule2]);
        });

        it('should not update rule in snapshot if running from snapshots', () => {
            rulesService.setup(x => x.runRuleFromSnapshots(app, rule1))
                .returns(() => of(true)).verifiable();

            rulesState.runFromSnapshots(rule1).subscribe();

            expect(rulesState.snapshot.rules).toEqual([rule1, rule2]);
        });

        it('should remove rule from snapshot if deleted', () => {
            rulesService.setup(x => x.deleteRule(app, rule1, rule1.version))
                .returns(() => of(versioned(newVersion))).verifiable();

            rulesState.delete(rule1).subscribe();

            expect(rulesState.snapshot.rules).toEqual([rule2]);
        });

        it('should invoke rule service if run is cancelled', () => {
            rulesService.setup(x => x.runCancel(app))
                .returns(() => of(true)).verifiable();

            rulesState.runCancel().subscribe();

            expect().nothing();
        });
    });

    describe('Selection', () => {
        beforeEach(() => {
            rulesService.setup(x => x.getRules(app))
                .returns(() => of(new DynamicRulesDto(({ items: [rule1, rule2], _links: {} })))).verifiable(Times.atLeastOnce());

            rulesState.load().subscribe();
            rulesState.select(rule2.id).subscribe();
        });

        it('should update selected rule if reloaded', () => {
            const newRules = [
                createRule(1, '_new'),
                createRule(2, '_new'),
            ];

            rulesService.setup(x => x.getRules(app))
                .returns(() => of(new DynamicRulesDto(({ items: newRules, _links: {} })))).verifiable(Times.exactly(2));

            rulesState.load().subscribe();

            expect(rulesState.snapshot.selectedRule).toEqual(newRules[1]);
        });

        it('should update selected rule if updated', () => {
            const request = new DynamicUpdateRuleDto({
                trigger: new ManualRuleTriggerDto(),
                action: {
                    actionType: 'action3',
                },
            });

            const updated = createRule(2, '_new');

            rulesService.setup(x => x.putRule(app, rule2, request, rule2.version))
                .returns(() => of(updated)).verifiable();

            rulesState.update(rule2, request).subscribe();

            expect(rulesState.snapshot.selectedRule).toEqual(updated);
        });

        it('should remove selected rule from snapshot if deleted', () => {
            rulesService.setup(x => x.deleteRule(app, rule2, rule2.version))
                .returns(() => of(versioned(newVersion))).verifiable();

            rulesState.delete(rule2).subscribe();

            expect(rulesState.snapshot.selectedRule).toBeNull();
        });
    });
});
