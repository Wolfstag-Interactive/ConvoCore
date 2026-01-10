import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'ConvoCore',
  url: 'https://docs.wolfstaginteractive.com',
  baseUrl: '/',

  presets: [
    [
      'classic',
      {
        docs: {
          routeBasePath: 'convocore',
          sidebarPath: require.resolve('./sidebars.ts'),
        },
        blog: false,
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    navbar: {
      title: 'ConvoCore',
      items: [
        {to: 'convocore/', label: 'Guide', position: 'left'},
        {href: 'convocore/api/index.html', label: 'API', position: 'left'},
      ],
    },
  },
};

export default config;