import globals from "globals";
import pluginJs from "@eslint/js";
import tseslint from "typescript-eslint";
import unusedImports from "eslint-plugin-unused-imports";

/** @type {import('eslint').Linter.Config[]} */
export default [
    {
        files: ["**/*.{js,mjs,cjs,ts,tsx}"],
        languageOptions: {
            globals: globals.browser
        }
    },

    pluginJs.configs.recommended,
    ...tseslint.configs.recommended,

    {
        plugins: {
            "unused-imports": unusedImports
        },
        rules: {
            "@typescript-eslint/no-explicit-any": "off",

            "@typescript-eslint/no-unused-vars": "off",
            "unused-imports/no-unused-imports": "error",
            "unused-imports/no-unused-vars": [
                "warn",
                {
                    vars: "all",
                    varsIgnorePattern: "^_",
                    args: "after-used",
                    argsIgnorePattern: "^_"
                }
            ],

            "no-empty": "warn"
        }
    }
];