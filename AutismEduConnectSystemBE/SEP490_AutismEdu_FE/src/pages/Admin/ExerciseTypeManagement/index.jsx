import { Box, Button, Card, CardContent, CardMedia, Grid, InputAdornment, Pagination, Stack, TextField, Typography, Modal, FormControl, InputLabel, Select, MenuItem, Tooltip, IconButton } from '@mui/material';
import React, { useEffect, useState } from 'react';
import SearchIcon from '@mui/icons-material/Search';

import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import ConfirmShow from './Modal/ConfirmShow';
import ExerciseTypeCreation from './Modal/ExerciseTypeCreation';
import EditIcon from '@mui/icons-material/Edit';
import ExerciseTypeEdit from './Modal/ExerciseTypeEdit';
const ExerciseTypeManagement = () => {
    const [loading, setLoading] = useState(false);
    const [exerciseTypeList, setExerciseTypeList] = useState([]);
    const [selectedExercise, setSelectedExercise] = useState(null);
    const [showId, setShowId] = useState(0);
    const [openModal, setOpenModal] = useState(false);
    const [dialogOpen, setDialogOpen] = useState(false);
    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 9,
        total: 10,
    });
    const [modalOpen, setModalOpen] = useState(false);
    const [modalDescription, setModalDescription] = useState('');
    const [filters, setFilters] = React.useState({
        search: '',
        isHide: 'all',
        orderBy: 'createdDate',
        sort: 'desc',
        pageSize: 9,
    });

    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const handleOpenModal = (description) => {
        setModalDescription(description);
        setModalOpen(true);
    };

    const handleCloseModal = () => {
        setModalOpen(false);
    };

    const handleFilterChange = (key) => (event) => {
        setFilters({
            ...filters,
            [key]: event.target.value,
        });
        setPagination({
            ...pagination,
            pageNumber: 1,
        });
    };

    useEffect(() => {
        handleGetExerciseTypeList();
    }, [filters, pagination.pageNumber]);

    const handleGetExerciseTypeList = async () => {
        try {
            setLoading(true);
            await services.ExerciseManagementAPI.getAllExerciseType((res) => {
                if (res?.result) {
                    setExerciseTypeList(res.result);
                    setPagination(res.pagination);
                }
            }, (error) => {
                console.log(error);

            }, {
                ...filters,
                pageNumber: pagination.pageNumber
            });

        } catch (error) {
            console.log(error);

        } finally {
            setLoading(false);
        }
    };

    const handleShow = (eType) => {
        if (!eType.isHide) return;
        setShowId(eType.id);
        setOpenModal(true);
    };

    const handleEdit = (eType) => {
        setSelectedExercise(eType);
        setDialogOpen(true);
        console.log(eType);
    };

    const handleShowExerciseType = async (showId) => {
        try {
            await services.ExerciseManagementAPI.changeStatus(showId, {}, (res) => {
                const currentEType = exerciseTypeList.map((eType) => {
                    if (eType.id === showId) {
                        return { ...eType, isHide: false };
                    } else {
                        return eType;
                    }
                });
                setExerciseTypeList(currentEType);
                enqueueSnackbar('Đổi trạng thái thành công!', { variant: 'success' });
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        } finally {
            setOpenModal(false);
        }
    };

    const handleCreateExerciseType = async (exerciseTypeData) => {
        try {
            await services.ExerciseManagementAPI.createExerciseType({ ...exerciseTypeData, exerciseTypeName: exerciseTypeData?.exerciseTypeName?.trim() }, (res) => {
                if (res?.result) {
                    if (pagination?.pageNumber === 1) {
                        const updatedList = [res.result, ...exerciseTypeList.slice(0, -1)];
                        setExerciseTypeList(updatedList);
                    } else {
                        setExerciseTypeList((prev) => [res.result, ...prev]);
                        setPagination((prev) => ({ ...prev, pageNumber: 1 }));
                    }
                    enqueueSnackbar('Tạo loại bài tập mới thành công!', { variant: 'success' });
                }
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: 'error' });
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        }
    };

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

    return (
        <Box sx={{
            height: (theme) => `calc(100vh - ${theme.myapp.adminHeaderHeight})`,
            p: 2,
        }}>
            <Box sx={{
                width: "100%", bgcolor: "white", p: "20px",
                borderRadius: "10px",
                boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px"
            }}>
                <Stack direction='column' sx={{
                    width: "90%",
                    height: exerciseTypeList?.length !== 0 ? 'auto' : '650px',
                    margin: "auto",
                    gap: 2
                }}>
                    <Typography variant='h4' textAlign={'center'} my={5}>Danh sách loại bài tập</Typography>

                    <Stack direction={'row'} alignItems={'center'} gap={2} mb={5}>
                        <Box width={'55%'}>
                            <TextField
                                fullWidth
                                size='small'
                                label="Tìm kiếm"
                                value={filters.search}
                                onChange={handleFilterChange('search')}
                                sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                                InputProps={{
                                    endAdornment: (
                                        <InputAdornment position="end">
                                            <SearchIcon />
                                        </InputAdornment>
                                    ),
                                }}
                            />
                        </Box>
                        <Box width={"20%"}>
                            <FormControl fullWidth size='small'>
                                <InputLabel id="sort-select-label">Thứ tự</InputLabel>
                                <Select
                                    labelId="sort-select-label"
                                    value={filters.sort}
                                    label="Thứ tự"
                                    onChange={handleFilterChange('sort')}
                                    sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                                >
                                    <MenuItem value="asc">Tăng dần theo ngày</MenuItem>
                                    <MenuItem value="desc">Giảm dần theo ngày</MenuItem>
                                </Select>
                            </FormControl>
                        </Box>
                        <Box width={"10%"}>
                            <FormControl fullWidth size='small'>
                                <InputLabel id="sort-select-label">Ẩn/Hiện</InputLabel>
                                <Select
                                    labelId="isHide-select-label"
                                    value={filters.isHide}
                                    label="Thứ tự"
                                    onChange={handleFilterChange('isHide')}
                                    sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                                >
                                    <MenuItem value="all">Tất cả</MenuItem>
                                    <MenuItem value="true">Ẩn</MenuItem>
                                    <MenuItem value="false">Hiện</MenuItem>
                                </Select>
                            </FormControl>
                        </Box>
                        <Box sx={{ width: "15%", display: 'flex' }}>
                            <Button size='medium' variant='contained' color='primary' onClick={() => setModalOpen(true)}>Tạo loại bài tập</Button>
                        </Box>
                    </Stack>

                    {exerciseTypeList?.length !== 0 ? <Grid container spacing={3} sx={{ flexWrap: 'wrap' }}>
                        {exerciseTypeList.map((eType, index) => (
                            <Grid item key={index} xs={12} sm={6} md={4}>
                                <Card
                                    sx={{
                                        maxWidth: 345,
                                        transition: 'transform 0.3s ease-in-out, box-shadow 0.3s ease-in-out',
                                        cursor: 'pointer',
                                        '&:hover': {
                                            transform: 'scale(1.05)',
                                            boxShadow: '0px 10px 15px rgba(0, 0, 0, 0.2)',
                                        },
                                    }}
                                >
                                    <CardMedia
                                        component="img"
                                        height="240"
                                        image="https://png.pngtree.com/png-vector/20190726/ourlarge/pngtree-college-education-graduation-cap-hat-university-icon-vector-desi-png-image_1588318.jpg"
                                        alt="Exercise Icon"
                                    />
                                    <CardContent>
                                        <Tooltip title={eType?.exerciseTypeName} placement='top'>
                                            <Typography
                                                variant="h6"
                                                component="div"
                                                sx={{
                                                    display: '-webkit-box',
                                                    WebkitLineClamp: 2,
                                                    WebkitBoxOrient: 'vertical',
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis',
                                                    height: '52px',
                                                }}
                                            >
                                                {eType?.exerciseTypeName}
                                            </Typography>
                                        </Tooltip>
                                        <Box sx={{ display: 'flex', justifyContent: 'right' }} gap={2}>
                                            <IconButton variant={'outlined'} color={'primary'} onClick={() => handleEdit(eType)}><EditIcon /></IconButton>
                                            <Button variant={!eType?.isHide ? 'outlined' : 'contained'} color={!eType?.isHide ? 'success' : 'warning'} onClick={() => handleShow(eType)}>{eType?.isHide ? 'Ẩn' : 'Hiện'}</Button>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>
                        ))}
                    </Grid> : 'Hiện không có loại bài tập nào!'}


                    {exerciseTypeList.length !== 0 && (<Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                        <Pagination
                            count={totalPages}
                            page={pagination.pageNumber}
                            onChange={handlePageChange}
                            color="primary"
                        />
                    </Stack>)}
                    <LoadingComponent open={loading} setOpen={setLoading} />
                    {openModal &&
                        <ConfirmShow open={openModal} handleClose={() => setOpenModal(false)} id={showId} exerciseTypeList={exerciseTypeList} setExerciseTypeList={setExerciseTypeList} handleShowExerciseType={handleShowExerciseType} />
                    }
                    {modalOpen && <ExerciseTypeCreation open={modalOpen} handleClose={() => setModalOpen(false)} handleCreateExerciseType={handleCreateExerciseType} />}
                    {dialogOpen && selectedExercise && <ExerciseTypeEdit open={dialogOpen} onClose={() => { setDialogOpen(false); setSelectedExercise(null); }} exerciseTypeList={exerciseTypeList} setExerciseTypeList={setExerciseTypeList} eType={selectedExercise} />}
                </Stack>
            </Box>
        </Box>
    )
}

export default ExerciseTypeManagement


