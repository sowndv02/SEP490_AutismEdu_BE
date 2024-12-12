import { Box } from '@mui/material';
import BigCity from './BigCity';
import Blog from './Blog';
import CarouselComponent from './CarouselComponent';
import TutorComponent from './Tutor';
function Home() {
    return (
        <Box sx={{ overflowX: 'hidden' }}>
            <CarouselComponent />
            <BigCity />
            <TutorComponent />
            <Blog />
        </Box>
    )
}

export default Home
