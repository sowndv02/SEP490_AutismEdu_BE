import { Box } from '@mui/material'
import React from 'react'
import Carousel from 'react-material-ui-carousel'
import image1 from '~/assets/images/carouselImages/image1.jpg'
import image2 from '~/assets/images/carouselImages/image2.jpg'
import image3 from '~/assets/images/carouselImages/image3.webp'
import image4 from '~/assets/images/carouselImages/image4.jpg'
function CarouselComponent() {
    var items = [
        image1,
        image2,
        image3,
        image4
    ]
    return (
        <Carousel>
            {
                items.map((src, i) => <Item key={i} src={src} />)
            }
        </Carousel>
    )
}

function Item({ src }) {
    return (
        <Box sx={{
            width: "100vw", height: {
                xs: "50vh",
                md: "70vh",
                lg: "90vh"
            }, backgroundImage: `url('${src}')`,
            backgroundSize: "cover",
            backgroundRepeat: "no-repeat"
        }}>
        </Box>
    )
}
export default CarouselComponent
