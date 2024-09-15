import { Box } from '@mui/material'
import React from 'react'
import Carousel from 'react-material-ui-carousel'
function CarouselComponent() {
    var items = [
        "https://media.baoquangninh.vn/dataimages/201804/original/images1045878_28828281_582036268796079_2141855731031247036_o.jpg",
        "https://trungtamnhanhoa.vn/wp-content/uploads/2022/12/Giao-vien-day-tre-cham-noi-can-thiep-tre-tu-ky-tre-tang-dong-giam-chu-y-Nhan-Hoa-1.jpg",
        "https://trungtamnhanhoa.vn/wp-content/uploads/2023/05/Can-thiep-ca-nhan-1-1-1.jpg"
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
                lg: "100vh"
            }, backgroundImage: `url('${src}')`,
            backgroundSize: "cover",
            backgroundRepeat: "no-repeat"
        }}>
        </Box>
    )
}
export default CarouselComponent
