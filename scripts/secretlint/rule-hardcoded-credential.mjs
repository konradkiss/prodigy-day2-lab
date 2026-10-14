/*
 * A secretlint rule for credentials the recommended preset has no vendor pattern for: a long literal assigned to a
 * name that says it is a secret (token, secret, password, api key, private key, connection string).
 *   const string StaffToken = "whs_live_...";   apiKey: 'abcd...'   PASSWORD=...
 */
const NAME = String.raw`[A-Za-z0-9_]*(?:token|secret|passw(?:or)?d|pwd|api_?key|private_?key|access_?key|connection_?string)[A-Za-z0-9_]*`;
const VALUE = String.raw`[A-Za-z0-9_\-+/=.~:@]{12,}`;
const ASSIGNED = new RegExp(String.raw`\b${NAME}["']?\s*(?:=|:)\s*(["'\x60]?)(${VALUE})\1`, 'gi');
/* Placeholders and references that are not secrets themselves. */
const NOT_A_SECRET = /^(?:\$\{|%|process\.env|Environment\.|<|your|example|changeme|xxx+|\*+)/i;

export const creator = {
  messages: {
    HARDCODED_CREDENTIAL: { en: () => 'found a hard-coded credential: read it from the environment or a secret store' },
  },
  meta: {
    id: 'lab/hardcoded-credential',
    recommended: true,
    type: 'scanner',
    supportedContentTypes: ['text'],
    docs: { url: 'https://github.com/secretlint/secretlint' },
  },
  create(context) {
    const t = context.createTranslator(creator.messages);
    return {
      file(source) {
        for (const match of source.content.matchAll(ASSIGNED)) {
          const value = match[2];
          if (NOT_A_SECRET.test(value) || !/\d/.test(value) || !/[A-Za-z]/.test(value)) continue;
          const start = (match.index ?? 0) + match[0].lastIndexOf(value);
          context.report({ message: t('HARDCODED_CREDENTIAL'), range: [start, start + value.length] });
        }
      },
    };
  },
};
