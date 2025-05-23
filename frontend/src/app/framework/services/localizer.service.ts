/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { Injectable } from '@angular/core';
import { compareStrings } from '../utils/array-helper';

@Injectable()
export class LocalizerService {
    constructor(
        private readonly translations: Object,
    ) {
    }

    public getOrKey(key: string | undefined, args?: any): string {
        return this.get(key, args) || key || '';
    }

    public get(key: string | undefined, args?: any): string | null {
        if (!key) {
            return null;
        }

        if (key.startsWith('i18n:')) {
            key = key.substring(5);
        }

        let text = (this.translations as any)[key];
        if (!text) {
            return null;
        }

        if (args && Object.keys(args).length > 0) {
            text = this.replaceVariables(text, args);
        }

        return text;
    }

    private replaceVariables(text: string, args: {}): string {
        text = text.replace(/{[^}]*}/g, (matched: string) => {
            const inner = matched.substring(1, matched.length - 1);

            let replaceValue: string;

            if (matched.includes('|')) {
                const splittedValue = inner.split('|');

                const key = splittedValue[0];

                replaceValue = this.getVar(args, key);

                if (replaceValue) {
                    const transforms = splittedValue.slice(1);

                    replaceValue = this.transform(replaceValue, transforms);
                }
            } else {
                replaceValue = this.getVar(args, inner);
            }

            return replaceValue;
        });

        return text;
    }

    private getVar(args: Record<string, string>, key: string) {
        let value = args[key];

        if (!value) {
            for (const name in args) {
                if (args.hasOwnProperty(name) && compareStrings(key, name) === 0) {
                    value = args[name];

                    break;
                }
            }
        }

        return value;
    }

    private transform(value: string, transforms: ReadonlyArray<string>) {
        for (const transform of transforms) {
            switch (transform) {
                case 'lower':
                    value = value.charAt(0).toLowerCase() + value.slice(1);
                    break;
                case 'upper':
                    value = value.charAt(0).toUpperCase() + value.slice(1);
                    break;
            }
        }

        return value;
    }
}
