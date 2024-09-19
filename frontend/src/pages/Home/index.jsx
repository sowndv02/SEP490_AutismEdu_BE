<<<<<<< HEAD
import React, { useState } from 'react';
import { AppBar, Tabs, Tab, Box } from '@mui/material';
import CarouselComponent from './CarouselComponent';
import BigCity from './BigCity';
import Center from './Center';
import TutorComponent from './Tutor';
import Blog from './Blog';
import AboutMe from './AboutMe';
function Home() {
    const [value, setValue] = useState(0);

    const handleChange = (event, newValue) => {
        setValue(newValue);
    };
=======
import { Box } from '@mui/material';
import AboutMe from './AboutMe';
import BigCity from './BigCity';
import Blog from './Blog';
import CarouselComponent from './CarouselComponent';
import Center from './Center';
import TutorComponent from './Tutor';
function Home() {
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    return (
        <Box>
            <CarouselComponent />
            <BigCity />
            <Center />
            <TutorComponent />
            <Blog />
            <AboutMe />
        </Box>
    )
}

export default Home
