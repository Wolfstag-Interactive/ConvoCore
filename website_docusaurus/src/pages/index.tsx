import React, {JSX, useEffect} from 'react';
import {useHistory} from '@docusaurus/router';

export default function Home(): JSX.Element | null {
    const history = useHistory();

    useEffect(() => {
        history.replace('/convocore/');
    }, [history]);

    return null;
}