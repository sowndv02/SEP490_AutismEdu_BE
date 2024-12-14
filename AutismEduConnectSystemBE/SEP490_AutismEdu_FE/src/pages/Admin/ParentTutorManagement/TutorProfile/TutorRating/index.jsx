import { Avatar, Box, Button, DialogActions, IconButton, Menu, MenuItem, Pagination, Rating, Stack, TextField, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import StarIcon from '@mui/icons-material/Star';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import 'dayjs/locale/vi';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
function TutorRating({ tutorId }) {

    const [open, setOpen] = useState(false);
    const [loading, setLoading] = useState(false);

    const [dataReviewStats, setDataReviewStats] = useState(null);
    const [pagination, setPagination] = React.useState({
        pageNumber: 1,
        pageSize: 10,
        total: 10,
    });

    useEffect(() => {
        handleGetDataReviewStats();
    }, []);


    console.log(dataReviewStats);

    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const handleGetDataReviewStats = async () => {
        try {
            await services.ReviewManagementAPI.getReviewStats(tutorId, (res) => {
                if (res?.result) {
                    console.log(res.result);
                    setDataReviewStats(res.result);
                }
            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };


    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

    dayjs.extend(relativeTime);
    dayjs.locale('vi');

    return (
        dataReviewStats && (
            <Box sx={{ width: "100%" }}>
                <Stack direction='row' gap={3}>
                    <Box sx={{ bgcolor: "#e4e9fd", px: 2, py: 3, width: "40%", borderRadius: "5px" }}>
                        <Typography variant='h6'>Đánh giá trung bình về gia sư</Typography>
                        <Typography><b style={{ fontWeight: "black", fontSize: '30px' }}>{dataReviewStats?.averageScore}</b>/5
                            <Rating name="text-feedback"
                                value={dataReviewStats?.averageScore}
                                readOnly
                                precision={0.1} />
                        </Typography>
                        <small>{dataReviewStats?.totalReviews} lượt đánh giá</small>
                    </Box>
                    <Box sx={{ width: "60%" }}>
                        <Typography variant='h6'>Phân tích đánh giá</Typography>
                        {dataReviewStats?.scoreGroups?.map((s, index) => {
                            if (s.scoreRange === '5') {
                                return (
                                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }} key={index}>
                                        <Typography sx={{ width: "2%" }}>5</Typography>
                                        <StarIcon />
                                        <Box sx={{ height: "15px", width: "60%", borderRadius: "10px", overflow: "hidden", boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px inset, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px inset" }}>
                                            <Box sx={{
                                                height: "15px",
                                                width: `calc(${(s.reviewCount / dataReviewStats?.totalReviews) * 100}%)`,
                                                bgcolor: '#5cb85c',
                                                borderRadius: "10px"
                                            }} />
                                        </Box>

                                        <Typography>{s.reviewCount}</Typography>
                                    </Stack>
                                );
                            } else if (s.scoreRange === '4') {
                                return (
                                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }} key={index}>
                                        <Typography sx={{ width: "2%" }}>4</Typography>
                                        <StarIcon />
                                        <Box sx={{ height: "15px", width: "60%", borderRadius: "10px", overflow: "hidden", boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px inset, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px inset" }}>
                                            <Box sx={{ bgcolor: "#428bca", width: `calc(${(s.reviewCount / dataReviewStats?.totalReviews) * 100}%)`, height: "15px" }} />
                                        </Box>
                                        <Typography>{s.reviewCount}</Typography>
                                    </Stack>
                                );
                            } else if (s.scoreRange === '3') {
                                return (
                                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }} key={index}>
                                        <Typography sx={{ width: "2%" }}>3</Typography>
                                        <StarIcon />
                                        <Box sx={{ height: "15px", width: "60%", borderRadius: "10px", overflow: "hidden", boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px inset, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px inset" }}>
                                            <Box sx={{ bgcolor: "#5bc0de", width: `calc(${(s.reviewCount / dataReviewStats?.totalReviews) * 100}%)`, height: "15px" }} />
                                        </Box>
                                        <Typography>{s.reviewCount}</Typography>
                                    </Stack>
                                );
                            } else if (s.scoreRange === '2') {
                                return (
                                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }} key={index}>
                                        <Typography sx={{ width: "2%" }}>2</Typography>
                                        <StarIcon />
                                        <Box sx={{ height: "15px", width: "60%", borderRadius: "10px", overflow: "hidden", boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px inset, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px inset" }}>
                                            <Box sx={{ bgcolor: "#f0ad4e", width: `calc(${(s.reviewCount / dataReviewStats?.totalReviews) * 100}%)`, height: "15px" }} />
                                        </Box>
                                        <Typography>{s.reviewCount}</Typography>
                                    </Stack>
                                );
                            } else {
                                return (
                                    <Stack direction='row' sx={{ alignItems: "center", gap: 1, width: "100%" }} key={index}>
                                        <Typography sx={{ width: "2%" }}>1</Typography>
                                        <StarIcon />
                                        <Box sx={{ height: "15px", width: "60%", borderRadius: "10px", overflow: "hidden", boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px inset, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px inset" }}>
                                            <Box sx={{ bgcolor: "#d9534f", width: `calc(${(s.reviewCount / dataReviewStats?.totalReviews) * 100}%)`, height: "15px" }} />
                                        </Box>
                                        <Typography>{s.reviewCount}</Typography>
                                    </Stack>
                                );
                            }
                        }

                        )}
                    </Box>
                </Stack>
                <Typography mt={3} variant='h4'>Đánh giá</Typography>

                {(dataReviewStats && dataReviewStats?.reviews?.length !== 0) ? dataReviewStats?.reviews?.map((r, index) => (
                    <Box bgcolor="#e4e9fd" p={2} sx={{ borderRadius: "5px", mt: 3 }} key={index}>
                        <Stack direction='row' mb={2} sx={{ justifyContent: "space-between", alignItems: 'center' }}>
                            <Stack direction='row' width={'80%'} sx={{ alignItems: "center", gap: 2 }}>
                                <Avatar src={r?.parent?.imageUrl || ''} alt="Remy Sharp" sx={{ width: "50px", height: "50px" }} />
                                <Box>
                                    <Typography variant='h6'>{r?.parent?.fullName || 'Khai XYZ'}</Typography>
                                    <Stack direction="row" alignItems="center">
                                        <Rating value={r?.rateScore} readOnly />
                                    </Stack>
                                </Box>
                            </Stack>
                            <Typography width={'15%'} textAlign={'right'}><small>{dayjs(new Date(r?.createdDate))?.fromNow()}</small></Typography>

                        </Stack>

                        <Typography variant='subtitle1'>{r?.description}</Typography>

                    </Box>

                )) : <Typography my={5} variant='subtitle1' textAlign={'center'}>Hiện tại chưa có đánh giá nào về gia sư!</Typography>
                }

                {
                    (dataReviewStats && dataReviewStats?.reviews?.length !== 0) &&
                    <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                        <Pagination
                            count={totalPages}
                            page={pagination.pageNumber}
                            onChange={handlePageChange}
                            color="primary"
                        />
                    </Stack>
                }

                <LoadingComponent open={loading} setOpen={setLoading} />
            </Box >
        )
    )
}

export default TutorRating;
