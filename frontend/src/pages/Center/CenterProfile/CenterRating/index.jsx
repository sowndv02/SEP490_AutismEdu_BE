import { Avatar, Box, Button, Pagination, Rating, Stack, Typography } from '@mui/material'
import React, { useState } from 'react'
import StarIcon from '@mui/icons-material/Star';
import { TextareaAutosize } from '@mui/base/TextareaAutosize';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime'
function CenterRating() {
    const [rating, setRating] = useState(1);
    dayjs.extend(relativeTime);
    return (
        <Box sx={{ width: "65%" }}>
            <Stack direction='row' gap={3}>
                <Box sx={{ bgcolor: "#e4e9fd", px: 2, py: 3, width: "40%", borderRadius: "5px" }}>
                    <Typography variant='h6'>Average center rating</Typography>
                    <Typography><b style={{ fontWeight: "black", fontSize: '30px' }}>4.3</b>/5
                        <Rating name="half-rating-read" defaultValue={4.7} precision={0.1} readOnly />
                    </Typography>
                    <small>3 reviews</small>
                </Box>
                <Box sx={{ width: "60%" }}>
                    <Typography variant='h6'>Rating breakdown</Typography>
                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }}>
                        <Typography sx={{ width: "2%" }}>5</Typography>
                        <StarIcon />
                        <Box sx={{ height: "15px", width: "60%", bgcolor: '#5cb85c', borderRadius: "10px" }}>
                        </Box>
                        <Typography>5</Typography>
                    </Stack>
                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }}>
                        <Typography sx={{ width: "2%" }}>4</Typography>
                        <StarIcon />
                        <Box sx={{ height: "15px", width: "60%", borderRadius: "10px", overflow: "hidden", boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px inset, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px inset" }}>
                            <Box sx={{ bgcolor: "#428bca", width: "80%", height: "15px" }}></Box>
                        </Box>
                        <Typography>5</Typography>
                    </Stack>
                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }}>
                        <Typography sx={{ width: "2%" }}>3</Typography>
                        <StarIcon />
                        <Box sx={{ height: "15px", width: "60%", borderRadius: "10px", overflow: "hidden", boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px inset, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px inset" }}>
                            <Box sx={{ bgcolor: "#5bc0de", width: "60%", height: "15px" }}></Box>
                        </Box>
                        <Typography>5</Typography>
                    </Stack>
                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }}>
                        <Typography sx={{ width: "2%" }}>2</Typography>
                        <StarIcon />
                        <Box sx={{ height: "15px", width: "60%", borderRadius: "10px", overflow: "hidden", boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px inset, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px inset" }}>
                            <Box sx={{ bgcolor: "#f0ad4e", width: "40%", height: "15px" }}></Box>
                        </Box>
                        <Typography>5</Typography>
                    </Stack>
                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }}>
                        <Typography sx={{ width: "2%" }}>1</Typography>
                        <StarIcon />
                        <Box sx={{ height: "15px", width: "60%", borderRadius: "10px", overflow: "hidden", boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px inset, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px inset" }}>
                            <Box sx={{ bgcolor: "#d9534f", width: "20%", height: "15px" }}></Box>
                        </Box>
                        <Typography>5</Typography>
                    </Stack>
                </Box>
            </Stack>
            <Typography mt={3} variant='h4'>Đánh giá</Typography>
            <Stack direction='row' mt={3} sx={{ alignItems: "start" }}>
                <Box sx={{
                    display: "flex",
                    alignItems: "center",
                    gap: 1,
                    width: "30%"
                }}>
                    <Avatar alt="Remy Sharp"
                        sx={{
                            width: "50px",
                            height: "50px"
                        }}
                        src="https://scontent.fhan2-4.fna.fbcdn.net/v/t39.30808-1/268142468_3035907700072578_4829229204736514171_n.jpg?stp=cp0_dst-jpg_s40x40&_nc_cat=100&ccb=1-7&_nc_sid=6738e8&_nc_eui2=AeFe_w7HSGpqFDepgviEP4pyq9KSuRzAWe6r0pK5HMBZ7pEuCwmHx3H-gP4TXxRF640CJIZj8zT62i8cDsbhFZrr&_nc_ohc=bFMv_CKAR0wQ7kNvgFHuqae&_nc_ht=scontent.fhan2-4.fna&_nc_gid=AGqxW37Vosru_hqYPbyxMG4&oh=00_AYBPmeiE9kQ9b7WqaSmV3ZgyMf5UJ_NkTcJz_inkboLHyQ&oe=66ED5045" />
                    <Typography variant='h6'>Khải Đào</Typography>
                </Box>
                <Box sx={{
                    width: "70%"
                }}>
                    <Rating
                        name="simple-controlled"
                        value={rating}
                        onChange={(event, newValue) => {
                            setRating(newValue);
                        }}
                    /> <br />
                    <TextareaAutosize minRows={5} maxRows={20} style={{ width: "100%" }} />
                    <Button variant='contained'>Đăng</Button>
                </Box>
            </Stack>
            <Box bgcolor="#e4e9fd" p={2} sx={{ borderRadius: "5px", mt: 3 }}>
                <Stack direction='row' sx={{
                    gap: 1,
                    justifyContent: "space-between"
                }}>
                    <Stack direction='row' sx={{
                        alignItems: "center",
                        gap: 1
                    }}>
                        <Avatar alt="Remy Sharp"
                            sx={{
                                width: "50px",
                                height: "50px"
                            }}
                            src="https://scontent.fhan2-4.fna.fbcdn.net/v/t39.30808-1/268142468_3035907700072578_4829229204736514171_n.jpg?stp=cp0_dst-jpg_s40x40&_nc_cat=100&ccb=1-7&_nc_sid=6738e8&_nc_eui2=AeFe_w7HSGpqFDepgviEP4pyq9KSuRzAWe6r0pK5HMBZ7pEuCwmHx3H-gP4TXxRF640CJIZj8zT62i8cDsbhFZrr&_nc_ohc=bFMv_CKAR0wQ7kNvgFHuqae&_nc_ht=scontent.fhan2-4.fna&_nc_gid=AGqxW37Vosru_hqYPbyxMG4&oh=00_AYBPmeiE9kQ9b7WqaSmV3ZgyMf5UJ_NkTcJz_inkboLHyQ&oe=66ED5045" />
                        <Box>
                            <Typography variant='h6'>Khải Đào</Typography>
                            <Typography>4.7  <Rating
                                defaultValue={5} readOnly /></Typography>
                        </Box>
                    </Stack>
                    <Typography><small>{dayjs(new Date("2024-05-05")).fromNow()}</small></Typography>
                </Stack>
                <Typography>Day.js is a minimalist JavaScript library that parses, validates, manipulates, and displays dates and times for modern browsers with a largely Moment.js-compatible API. If you use Moment.js, you already know how to use Day.js.</Typography>
            </Box>
            <Pagination count={10} color="primary" sx={{ my: "30px" }} />
        </Box >
    )
}

export default CenterRating
