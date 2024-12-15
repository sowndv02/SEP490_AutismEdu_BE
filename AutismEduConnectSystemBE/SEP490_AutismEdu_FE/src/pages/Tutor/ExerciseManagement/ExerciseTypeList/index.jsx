import { Box, Card, CardContent, CardMedia, Grid, InputAdornment, Pagination, Stack, TextField, Tooltip, Typography } from '@mui/material';
import React, { useEffect, useState } from 'react';
import SearchIcon from '@mui/icons-material/Search';
import ExerciseList from './ExerciseList';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
import emptyBook from '~/assets/images/icon/emptybook.gif'
import imgExerciseType from '~/assets/images/exercisetype.jpg'

function ExerciseTypeList() {
    const [loading, setLoading] = useState(false);
    const [search, setSearch] = useState('');
    const [showExerciseList, setShowExerciseList] = useState(false);
    const [selected, setSelected] = useState(null);
    const [exerciseTypes, setExerciseTypes] = useState([]);
    const [pagination, setPagination] = React.useState({
        pageNumber: 1,
        pageSize: 10,
        total: 10,
    });
    console.log(exerciseTypes);


    useEffect(() => {
        handleGetAllExerciseType();
    }, []);

    useEffect(() => {
        setTimeout(() => {
            handleGetAllExerciseType();
            window.scrollTo(0, 0);
        }, 500);
    }, [search, pagination.pageNumber]);

    const handleGetAllExerciseType = async () => {
        try {
            setLoading(true);
            await services.ExerciseManagementAPI.getListExerciseType((res) => {
                if (res?.result) {
                    setExerciseTypes(res.result);
                    setPagination(res.pagination);
                }
            }, (error) => {
                console.log(error);
            }, {
                search,
                isHide: 'false',
                orderBy: 'createdDate',
                sort: 'desc',
                pageSize: 9,
                pageNumber: pagination.pageNumber
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    }

    const handleSearch = (e) => {
        const { value } = e.target;
        setSearch(value);
        setPagination((prev) => ({ ...prev, pageNumber: 1 }));
    };


    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const handleGetExerciseList = (type) => {
        setSelected(type);
        setShowExerciseList(true);
    }

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);


    if (showExerciseList && selected) {
        return <ExerciseList selectedExerciseType={selected} setShowExerciseList={setShowExerciseList} />
    }

    return (
        <Stack direction='column' sx={{
            width: "90%",
            margin: "auto",
            gap: 2
        }}>
            <Typography variant='h4' textAlign={'center'} my={2}>Danh sách loại bài tập</Typography>

            <Box width={'60%'} margin={'auto'} mb={2}>
                <TextField
                    fullWidth
                    size='small'
                    label="Tìm kiếm"
                    value={search}
                    onChange={handleSearch}
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

            <Grid container spacing={3} sx={{ flexWrap: 'wrap' }}>
                {exerciseTypes.map(type => (
                    <Grid item key={type.id} xs={12} sm={6} md={4}>
                        <Card
                            sx={{
                                maxWidth: 345,
                                textAlign: 'center',
                                transition: 'transform 0.3s ease-in-out, box-shadow 0.3s ease-in-out',
                                cursor: 'pointer',
                                '&:hover': {
                                    transform: 'scale(1.05)',
                                    boxShadow: '0px 10px 15px rgba(0, 0, 0, 0.2)',
                                },
                            }}
                            onClick={() => handleGetExerciseList(type)}
                        >
                            <CardMedia
                                component="img"
                                height="240"
                                image={imgExerciseType}
                                // image="https://png.pngtree.com/png-vector/20190726/ourlarge/pngtree-college-education-graduation-cap-hat-university-icon-vector-desi-png-image_1588318.jpg"
                                alt="Exercise Icon"
                            />
                            <CardContent>
                                <Tooltip title={type.exerciseTypeName} placement="top-start">
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
                                        {type.exerciseTypeName}
                                    </Typography>
                                </Tooltip>
                                {/* <Typography variant='body2'>(Số lượng bài tập: <b>{type?.exercises?.length}</b>)</Typography> */}
                            </CardContent>
                        </Card>
                    </Grid>
                ))}
            </Grid>
            {exerciseTypes.length !== 0 && <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                <Pagination
                    count={totalPages}
                    page={pagination.pageNumber}
                    onChange={handlePageChange}
                    color="primary"
                />
            </Stack>}
            {exerciseTypes.length === 0 && <Box sx={{ textAlign: "center" }}>
                <img src={emptyBook} style={{ height: "200px" }} />
                <Typography>Hiện không loại bài tập nào!</Typography>
            </Box>}
            <LoadingComponent open={loading} setOpen={setLoading} />

        </Stack>
    );
}

export default ExerciseTypeList;
