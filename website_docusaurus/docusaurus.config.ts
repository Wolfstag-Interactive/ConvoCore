import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'ConvoCore',
  tagline: 'A modular Unity dialogue framework',
  url: 'https://docs.wolfstaginteractive.com',
  baseUrl: '/',

  plugins: [
    [
      require.resolve("@easyops-cn/docusaurus-search-local"),
      {
        hashed: true,
        language: ["en"],
        indexDocs: true,
        indexBlog: false,
        indexPages: false,
        docsRouteBasePath: "/convocore",
      },
    ],
  ],

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
        {to: '/convocore/', label: 'Guide', position: 'left'},
        {href: 'https://docs.wolfstaginteractive.com/convocore/api/', label: 'API', position: 'left'},
      ],
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['yaml', 'csharp'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
