import { Box } from '@mui/material';
import AboutMe from './AboutMe';
import BigCity from './BigCity';
import Blog from './Blog';
import CarouselComponent from './CarouselComponent';
import Center from './Center';
import TutorComponent from './Tutor';
function Home() {
    return (
        <Box>
            <CarouselComponent />
            <BigCity />
            <TutorComponent />
            <Blog />
            <AboutMe />
        </Box>
    )
}

export default Home
