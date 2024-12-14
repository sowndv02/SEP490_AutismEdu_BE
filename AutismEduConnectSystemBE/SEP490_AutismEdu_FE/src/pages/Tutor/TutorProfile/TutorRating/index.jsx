import { Avatar, Box, Button, DialogActions, FormHelperText, IconButton, Menu, MenuItem, Pagination, Rating, Stack, TextField, Typography } from '@mui/material'
import React, { memo, useEffect, useState } from 'react'
import StarIcon from '@mui/icons-material/Star';
import { TextareaAutosize } from '@mui/base/TextareaAutosize';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import MoreHorizIcon from '@mui/icons-material/MoreHoriz';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import LoadingComponent from '~/components/LoadingComponent';
import DeleteConfirmationModal from './RatingModal/DeleteConfirmationModal';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import ReportIcon from '@mui/icons-material/Report';
import ReportModal from './ReportReview';
function TutorRating({ tutorId, userInfo }) {
    console.log('re-render rating');
    const [ratingData, setRatingData] = useState({
        rateScore: 0,
        description: '',
        tutorId: tutorId
    });
    const [idDelete, setIdDelete] = useState(-1);
    const [open, setOpen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [selectedReview, setSelectedReview] = useState(null);
    const [tempRating, setTempRating] = useState(0);
    const [tempContent, setTempContent] = useState('');
    const [openReportReview, setOpenReportReview] = useState(false);
    const [currentReport, setCurrentReport] = useState(null);
    const [dataReviewStats, setDataReviewStats] = useState(null);
    const [studyingList, setStudyingList] = useState([]);
    const [isLearned, setIsLearned] = useState(false);
    const [pagination, setPagination] = React.useState({
        pageNumber: 1,
        pageSize: 10,
        total: 10,
    });


    const [anchorEl, setAnchorEl] = useState(null);
    const [anchorElR, setAnchorElR] = useState(null);
    const [isSaveDisabled, setIsSaveDisabled] = useState(true);

    const [isDisabled, setIsDisabled] = useState(true);
    const [errors, setErrors] = useState({});

    const validateForm = () => {
        const {
            description
        } = ratingData;

        const newErrors = {};

        if (isEditing) {
            if (tempContent.trim().length > 500) {
                newErrors.tempContent = 'Không được vượt quá 500 ký tự';
            }
        } else {
            if (description.trim().length > 500) {
                newErrors.description = 'Không được vượt quá 500 ký tự';
            }
        }
        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    useEffect(() => {
        if (ratingData.description || tempContent) {
            setIsDisabled(!validateForm());
        }
    }, [ratingData.description, tempContent]);

    useEffect(() => {
        handleGetDataReviewStats();
        if (userInfo) {
            handleGetStudyingList();
        }
    }, [tutorId, userInfo]);

    useEffect(() => {
        if (selectedReview) {
            setTempRating(selectedReview?.rateScore);
            setTempContent(selectedReview?.description);
        }
    }, [selectedReview]);

    useEffect(() => {
        if (studyingList.length !== 0) {
            const isExist = studyingList.some((s) => s?.tutorId === tutorId);
            console.log(isExist);
            setIsLearned(isExist);
        }
    }, [studyingList, tutorId]);

    console.log(isLearned);

    const handleChangeRatingData = (e) => {
        const { name, value } = e.target;
        setRatingData((prev) => ({ ...prev, [name]: value }));
    };

    const handleOpenMenu = (event) => {
        setAnchorEl(event.currentTarget);
    };
    const handleOpenMenuR = (event) => {
        setAnchorElR(event.currentTarget);
    };

    const handleCloseMenu = () => {
        setAnchorEl(null);
    };
    const handleCloseMenuR = () => {
        setAnchorElR(null);
    };

    const handleEditClick = (r) => {
        setSelectedReview(r);
        setIsEditing(true);
        handleCloseMenu();
    };

    const handleClickOpen = (id) => {
        setIdDelete(id);
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };

    const handleCancle = () => {
        setSelectedReview(null);
        setIsEditing(false);
    };

    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    useEffect(() => {
        if (selectedReview) {
            setIsSaveDisabled(tempRating === selectedReview.rateScore && tempContent === selectedReview.description);
        }
    }, [tempRating, tempContent, selectedReview]);

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

    const handleGetStudyingList = async () => {
        try {
            await services.StudentProfileAPI.getMyTutor((res) => {
                if (res?.result) {
                    const newData = res?.result?.filter((r) => r.tutorId === tutorId);
                    setStudyingList(newData);
                }
            }, (error) => {
                console.log(error);
            }, {
                status: 'all'
            });
        } catch (error) {
            console.log(error);
        }
    };

    const handleSubmitRating = async () => {
        try {
            setLoading(true);
            await services.ReviewManagementAPI.createReview({
                ...ratingData, description: ratingData?.description?.trim()
            }, async (res) => {
                if (res?.result) {
                    const addData = [res.result, ...dataReviewStats.reviews];
                    setDataReviewStats((prev) => ({ ...prev, reviews: addData }));
                    setRatingData((prev) => ({ ...prev, rateScore: 0, description: '' }))
                    enqueueSnackbar("Đánh giá đã được đăng thành công!", { variant: 'success' });
                    await handleGetDataReviewStats();
                }
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: 'error' });
                console.log(error);
            });
        } catch (error) {
            console.log(error);

        } finally {
            setIsDisabled(true);
            setLoading(false);
        }
    };

    const handleSaveEdit = async () => {
        try {
            const updateData = {
                rateScore: tempRating,
                description: tempContent.trim()
            };
            setLoading(true);
            await services.ReviewManagementAPI.updateReview(selectedReview?.id, updateData, async (res) => {
                if (res?.result) {
                    const updatedReviews = dataReviewStats?.reviews?.map((r) =>
                        r.id === res.result.id ? res.result : r
                    );
                    setDataReviewStats((prev) => ({ ...prev, reviews: updatedReviews }));
                    enqueueSnackbar('Cập nhật đánh giá thành công!', { variant: 'success' });
                    await handleGetDataReviewStats();
                }
            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        } finally {
            setIsDisabled(true);
            setSelectedReview(null);
            setIsEditing(false);
            setLoading(false);
        }
    };


    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

    // const isReviewExist = dataReviewStats?.reviews?.includes(userInfo?.id);
    const checkReviewExist = () => {
        if (dataReviewStats?.reviews) {
            const arrayReview = dataReviewStats.reviews?.map((r) => r?.parent?.id);
            return arrayReview?.includes(userInfo?.id);
        }
    };

    const isRevewExist = checkReviewExist();

    dayjs.extend(relativeTime);
    dayjs.locale('vi');

    console.log(dataReviewStats?.reviews);
    console.log(userInfo);

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
                {userInfo && !isRevewExist && isLearned && <Stack direction='row' mt={3} sx={{ alignItems: "start" }}>
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
                            src={userInfo?.imageUrl} />
                        <Typography variant='h6'>{userInfo?.fullName}</Typography>
                    </Box>
                    <Stack direction='column' sx={{
                        width: "70%",
                        gap: 3
                    }}>
                        <Rating
                            name="rateScore"
                            value={ratingData.rateScore}
                            onChange={handleChangeRatingData}
                        />
                        <Box>
                            <TextField
                                id="outlined-multiline-static"
                                label="Đánh giá"
                                name='description'
                                multiline
                                rows={4}
                                sx={{ width: "100%" }}
                                value={ratingData.description || ''}
                                onChange={handleChangeRatingData}
                            />
                            {errors.description ? (
                                <FormHelperText error>{errors.description}</FormHelperText>
                            ) : <Typography variant='caption'>{ratingData?.description?.trim()?.length}/500</Typography>}
                        </Box>
                        <Button variant='contained' disabled={!ratingData.rateScore || isDisabled} onClick={handleSubmitRating}>Đăng</Button>
                    </Stack>
                </Stack>}

                {
                    (dataReviewStats && dataReviewStats?.reviews?.length !== 0) ? dataReviewStats?.reviews?.map((r, index) =>
                    (
                        <Box bgcolor="#e4e9fd" p={2} sx={{ borderRadius: "5px", mt: 3 }} key={index}>
                            <Stack direction='row' mb={2} sx={{ justifyContent: "space-between", alignItems: 'center' }}>
                                <Stack direction='row' width={'80%'} sx={{ alignItems: "center", gap: 2 }}>
                                    <Avatar src={r?.parent?.imageUrl || ''} alt="Remy Sharp" sx={{ width: "50px", height: "50px" }} />
                                    <Box>
                                        <Typography variant='h6'>{r?.parent?.fullName || 'Khai XYZ'}</Typography>
                                        <Stack direction="row" alignItems="center">
                                            {isEditing && (r?.id === selectedReview?.id) ? (
                                                <Rating
                                                    value={tempRating}
                                                    onChange={(e, newRating) => setTempRating(newRating)}
                                                />
                                            ) : (
                                                <Rating value={r?.rateScore} readOnly />
                                            )}
                                        </Stack>
                                    </Box>
                                </Stack>
                                <Typography width={'15%'} textAlign={'right'}><small>{dayjs(new Date(r?.updatedDate ?? r?.createdDate))?.fromNow()}</small></Typography>

                                {
                                    userInfo && ((userInfo?.id === r?.parent?.id) ? (
                                        <IconButton onClick={handleOpenMenu} size='medium'>
                                            <MoreHorizIcon />
                                        </IconButton>
                                    ) : <IconButton onClick={handleOpenMenuR} size='medium'>
                                        <MoreHorizIcon />
                                    </IconButton>)
                                }
                                <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleCloseMenu}>
                                    <MenuItem onClick={() => handleEditClick(r)}>
                                        <EditIcon fontSize="small" color='primary' sx={{ mr: 1 }} />
                                        Chỉnh sửa
                                    </MenuItem>
                                    <MenuItem onClick={() => { handleClickOpen(r?.id); handleCloseMenu(); }}>
                                        <DeleteIcon fontSize="small" color='error' sx={{ mr: 1 }} />
                                        Xoá
                                    </MenuItem>
                                </Menu>
                                <Menu anchorEl={anchorElR} open={Boolean(anchorElR)} onClose={handleCloseMenuR}>
                                    <MenuItem onClick={() => { setOpenReportReview(true); setCurrentReport(r); handleCloseMenuR(); }}>
                                        <ReportIcon fontSize="small" color='warning' sx={{ mr: 1 }} />
                                        Tố cáo
                                    </MenuItem>
                                </Menu>
                            </Stack>

                            {isEditing && (r?.id === selectedReview?.id) ? (
                                <Box>
                                    <TextField
                                        multiline
                                        fullWidth
                                        value={tempContent}
                                        onChange={(e) => setTempContent(e.target.value)}
                                        sx={{ mt: 1 }}
                                    />
                                    {errors.tempContent ?
                                        <FormHelperText error>{errors.tempContent}</FormHelperText>
                                        : <Typography variant='caption'>{tempContent?.trim()?.length}/500</Typography>}
                                </Box>
                            ) : (
                                <Typography variant='subtitle1'>{r?.description}</Typography>
                            )}

                            {(isEditing && (r?.id === selectedReview?.id)) && (
                                <DialogActions>
                                    <Button onClick={handleCancle} variant='outlined' color='inherit'>Hủy</Button>
                                    <Button
                                        onClick={handleSaveEdit}
                                        color="primary"
                                        variant="contained"
                                        disabled={isSaveDisabled || isDisabled || !tempContent}
                                    >
                                        Lưu
                                    </Button>
                                </DialogActions>
                            )}

                        </Box>

                    )
                    ) : <Typography my={5} variant='subtitle1' textAlign={'center'}>Hiện tại chưa có đánh giá nào về gia sư.</Typography>
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

                {open && <DeleteConfirmationModal id={idDelete} open={open} handleClose={handleClose} dataReviewStats={dataReviewStats} setDataReviewStats={setDataReviewStats} handleGetDataReviewStats={handleGetDataReviewStats} />}

                <LoadingComponent open={loading} setOpen={setLoading} />
                <ReportModal open={openReportReview} setOpen={setOpenReportReview} currentReport={currentReport} />
            </Box >
        )
    )
}

export default memo(TutorRating);
