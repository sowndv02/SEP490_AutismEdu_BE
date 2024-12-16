import React, { useState, useEffect } from 'react';
import { Box, Button, Card, CardActionArea, CardContent, Grid, InputAdornment, IconButton, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography, MenuItem, Select, FormControl, InputLabel, Dialog, DialogTitle, DialogContent, DialogActions, Pagination, Divider } from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import LoadingComponent from '~/components/LoadingComponent';
import ExerciseUpdateModal from './ExerciseModal/ExerciseUpdateModal';
// import DeleteConfirmationModal from './ExerciseModal/DeleteConfirmationModal';
import ExerciseCreation from './ExerciseModal/ExerciseCreation';
import services from '~/plugins/services';
import { format } from 'date-fns';
import { enqueueSnackbar } from 'notistack';
import emptyBook from '~/assets/images/icon/emptybook.gif'
import DeleteConfirmationModal from './ExerciseModal/DeleteConfirmationModal';

function ExerciseList({ selectedExerciseType, setShowExerciseList }) {

    const [dataFilter, setDataFilter] = useState({
        search: '',
        orderBy: 'createdDate',
        sort: 'desc'
    });
    const [exercises, setExercises] = useState([]);
    const [openDialog, setOpenDialog] = useState(false);
    const [selectedContent, setSelectedContent] = useState('');
    const [loading, setLoading] = useState(false);
    const [openEditDialog, setOpenEditDialog] = useState(false);
    const [selectedExercise, setSelectedExercise] = useState(null);
    const [openDeleteConfirm, setOpenDeleteConfirm] = useState(false);
    const [openCreation, setOpenCreation] = useState(false);
    const [currentDeleteIndex, setCurrentDeleteIndex] = useState(null);
    const [done, setDone] = useState(false);

    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 10,
        total: 10,
    });


    useEffect(() => {
        handleGetExerciseByTypeId();
    }, [dataFilter, pagination.pageNumber]);

    const handleGetExerciseByTypeId = async () => {
        try {
            setLoading(true);
            await services.ExerciseManagementAPI.getExerciseByTypeId(selectedExerciseType?.id, (res) => {
                if (res?.result) {
                    setExercises(res.result);
                    setPagination(res.pagination);
                }
            }, (error) => {
                console.log(error);
            }, {
                search: dataFilter.search,
                pageNumber: pagination.pageNumber,
                orderBy: 'createdDate',
                sort: dataFilter.sort
            })
        } catch (error) {
            console.log(error);
        } finally {
            setDone(true);
            setLoading(false);
        }
    }

    const handleChangeDataFilter = (e) => {
        const { name, value } = e.target;
        setDataFilter((prev) => ({ ...prev, [name]: value }));
        setPagination({
            ...pagination,
            pageNumber: 1,
        });
    }

    const handleOpenDialog = (content) => {
        setSelectedContent(content);
        setOpenDialog(true);
    };

    const handleCloseDialog = () => {
        setOpenDialog(false);
        setSelectedContent('');
    };

    const handleOpenEditDialog = (exercise) => {
        setSelectedExercise(exercise);
        setOpenEditDialog(true);
    };

    const handleCloseEditDialog = () => {
        setOpenEditDialog(false);
        setSelectedExercise(null);
    };

    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const handleOpenDelete = (index) => {
        setCurrentDeleteIndex(index);
        setOpenDeleteConfirm(true);
    }

    const handleDeleteExercise = async () => {
        try {
            setLoading(true);
            await services.ExerciseManagementAPI.deleteExercise(currentDeleteIndex, {}, (res) => {
                const newExercise = exercises.filter((e) => e.id !== currentDeleteIndex);
                setExercises(newExercise);
                enqueueSnackbar("Xoá bài tập thành công thành công", { variant: 'success' });
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
            setOpenDeleteConfirm(false);
        }
    };

    const formatDate = (dateString) => {
        return format(new Date(dateString), 'dd/MM/yyyy');
    };


    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

    return (
        <Stack direction='column' sx={{ width: "90%", margin: "auto", gap: 2 }}>
            <Box sx={{ display: 'flex' }} mt={1}>
                <Button variant='contained' startIcon={<ArrowBackIcon />} onClick={() => setShowExerciseList(false)}>Quay lại</Button>
            </Box>
            <Typography variant='h4' textAlign={'center'} my={2}>Danh sách bài tập</Typography>


            <Box display="flex" justifyContent="space-between" alignItems="center" width="100%" mb={2}>
                <TextField
                    disabled={(exercises.length === 0 && dataFilter.search === '')}
                    size='small'
                    label="Tìm kiếm"
                    name="search"
                    value={dataFilter.search}
                    onChange={handleChangeDataFilter}
                    InputProps={{
                        endAdornment: (
                            <InputAdornment position="end">
                                <SearchIcon />
                            </InputAdornment>
                        ),
                    }}
                    sx={{ backgroundColor: '#fff', borderRadius: '4px', width: '50%' }}
                />

                <Box sx={{ width: '50%', display: 'flex', justifyContent: 'flex-end' }} gap={2}>
                    <FormControl fullWidth size='small' sx={{ width: '40%' }}>
                        <InputLabel id="sort-select-label">Thứ tự</InputLabel>
                        <Select
                            disabled={exercises.length === 0}
                            name='sort'
                            value={dataFilter.sort}
                            onChange={handleChangeDataFilter}
                            size='small'
                            label='Thứ tự'
                            sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                        >
                            <MenuItem value="asc">Tăng dần theo ngày</MenuItem>
                            <MenuItem value="desc">Giảm dần theo ngày</MenuItem>
                        </Select>
                    </FormControl>
                    <Button variant='contained' color='primary' onClick={() => setOpenCreation(true)}>Tạo bài tập</Button>
                </Box>

            </Box>

            <Typography variant='h6'>Loại bài tập: <span style={{ fontWeight: '100' }}>{selectedExerciseType?.exerciseTypeName}</span></Typography>


            {done && ((done && exercises.length !== 0) ? <>
                <TableContainer component={Paper} sx={{ mt: 3, boxShadow: 3, borderRadius: 2 }}>
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell sx={{ fontWeight: 'bold' }}>STT</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Tên bài tập</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Ngày tạo</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Hành động</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {exercises.map((exercise, index) => (
                                <TableRow key={exercise.id} hover>
                                    <TableCell>{index + 1 + (pagination?.pageNumber - 1) * pagination?.pageSize}</TableCell>
                                    <TableCell
                                        onClick={() => handleOpenDialog(exercise.description)}
                                        sx={{ maxWidth: '350px' }}
                                    >
                                        <Box sx={{
                                            overflow: 'hidden',
                                            textOverflow: 'ellipsis',
                                            whiteSpace: 'nowrap',
                                            maxWidth: '100%',
                                            color: 'primary.main',
                                            cursor: 'pointer',
                                            '&:hover': {
                                                textDecoration: 'underline'
                                            }
                                        }}>
                                            {exercise.exerciseName}
                                        </Box>
                                    </TableCell>
                                    <TableCell>{formatDate(exercise?.createdDate)}</TableCell>
                                    <TableCell>
                                        <IconButton color="primary" aria-label="chỉnh sửa" onClick={() => handleOpenEditDialog(exercise)}>
                                            <EditIcon />
                                        </IconButton>
                                        <IconButton color="error" aria-label="xoá" onClick={() => handleOpenDelete(exercise.id)}>
                                            <DeleteIcon />
                                        </IconButton>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>

                <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                    <Pagination
                        count={totalPages}
                        page={pagination.pageNumber}
                        onChange={handlePageChange}
                        color="primary"
                    />
                </Stack>
            </> : <Box sx={{ textAlign: "center" }}>
                <img src={emptyBook} style={{ height: "200px" }} />
                <Typography>Hiện tại chưa có bài tập nào!</Typography>
            </Box>)}

            <LoadingComponent open={loading} setOpen={setLoading} />

            <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
                <DialogTitle variant='h5' textAlign={'center'}>Nội dung bài tập</DialogTitle>
                <Divider />
                <DialogContent>
                    <Box
                        width={'100%'}
                        maxHeight={'400px'}
                        overflowY={'auto'}
                        overflowX={'hidden'}
                        sx={{
                            padding: 2,
                            border: '1px solid #ccc',
                            borderRadius: '4px',
                            wordBreak: 'break-word',
                            overflowWrap: 'break-word',
                        }}
                        dangerouslySetInnerHTML={{ __html: selectedContent }}
                    />
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={handleCloseDialog} variant='contained' color="primary">Đóng</Button>
                </DialogActions>
            </Dialog>

            {openCreation && <ExerciseCreation setExercises={setExercises} exerciseType={selectedExerciseType} open={openCreation} handleClose={() => setOpenCreation(false)} />}
            {openEditDialog && <ExerciseUpdateModal exercises={exercises} setExercises={setExercises} openEditDialog={openEditDialog} handleCloseEditDialog={handleCloseEditDialog} selectedExercise={selectedExercise} setSelectedExercise={setSelectedExercise} selectedExerciseType={selectedExerciseType} />}
            {openDeleteConfirm && <DeleteConfirmationModal open={openDeleteConfirm} handleClose={() => { setOpenDeleteConfirm(false) }} handleDelete={handleDeleteExercise} />}
        </Stack>
    );
}

export default ExerciseList;
